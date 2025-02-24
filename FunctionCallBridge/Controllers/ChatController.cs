using FunctionCallBridge.OpenAIModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;


namespace FunctionCallBridge.Controllers
{
    [ApiController]
    public class ChatController : ControllerBase
    {
        /// <summary>
        /// 对话接口
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("v1/chat/completions")]
        public async Task<IActionResult> Chat(OpenAIRequest model)
        {
            Request.Headers.TryGetValue("Authorization", out var apiKey);
            if (IsCall(model))
            {
                //如果已经调用过

                foreach (var message in model.messages)
                {
                    if (message.content == null)
                    {
                        message.content = "开始调用函数";
                    }
                    if (message.role == "tool")
                    {
                        message.role = "assistant";
                    }
                }

                //model.messages.RemoveAll(m => m.content == null);

                model.tools = null;
                return await ProcessRequest(model, apiKey, model.stream);
            }
            if (NeedsFunctionCallProcessing(model))
            {
                BuildFunctionCallPrompt(model);
                return await ProcessRequest(model, apiKey, false,false,true);
            }
            else if (NeedsJsonProcessing(model))
            {
                BuildJsonPrompt(model);
                return await ProcessRequest(model, apiKey, false, false);
            }
            else
            {
                model.tools = null;
                return await ProcessRequest(model, apiKey, model.stream);
            }
        }


        private bool IsCall(OpenAIRequest model)
        {
            bool flag = false; ;
            foreach (var message in model.messages)
            {
                if (message.role == "tool")
                {
                    return true;
                }
            }
            return flag;
        }

        // 判断是否需要处理 function call 和 json_object 参数的方法
        private bool NeedsFunctionCallProcessing(OpenAIRequest model)
        {
            // 根据实际情况判断
            // 例如，检查 model 中是否包含特定的 function 或者参数类型
            //foreach (var message in model.Messages)
            //{
            //    if (message.Role == "tool")
            //    {
            //        return false;
            //    }
            //}
            return (model.tools!=null&&model.tools.Count>0); // 根据需求返回 true 或 false
        }

        private bool NeedsJsonProcessing(OpenAIRequest model)
        {
            // 根据实际情况判断是否需要处理 JSON
            // 例如，检查 model 中是否包含特定的 JSON 参数
            return model.response_format?.type== "json_object";
        }

        private async Task<IActionResult> ProcessRequest(OpenAIRequest model, string apiKey, bool stream,bool isThink=true,bool isFc=false)
        {
            Console.WriteLine("请求:");
            Console.WriteLine(JsonConvert.SerializeObject(model, Formatting.Indented));
            if (stream)
            {
                Response.ContentType = "text/event-stream";
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("X-Accel-Buffering", "no");

                using var httpClient = new HttpClient();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var requestcontent = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                using var requestmessage = new HttpRequestMessage(HttpMethod.Post, AIOption.Endpoint)
                {
                    Content = requestcontent
                };

                using var responsemessage = await httpClient.SendAsync(requestmessage, HttpCompletionOption.ResponseHeadersRead);

                if (responsemessage.IsSuccessStatusCode)
                {
                    var streamResponse = await responsemessage.Content.ReadAsStreamAsync();
                    using var reader = new StreamReader(streamResponse);

                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var sseData = $"{line}\n\n";
                            var dataBytes = Encoding.UTF8.GetBytes(sseData);
                            await Response.Body.WriteAsync(dataBytes, 0, dataBytes.Length);
                            await Response.Body.FlushAsync();
                        }
                    }
                    return new EmptyResult();
                }
                else
                {
                    var errorcontent = await responsemessage.Content.ReadAsStringAsync();
                    return StatusCode((int)responsemessage.StatusCode, errorcontent);
                }
            }
            else
            {
                Response.ContentType = "application/json";
                var client = new RestClient();
                var request = new RestRequest(AIOption.Endpoint, Method.Post);
                request.AddHeader("content-Type", "application/json");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.AddHeader("Authorization", $"Bearer {apiKey}");
                }
                request.AddJsonBody(model);

                var response = await client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    ChatCompletion chatCompletion = JsonConvert.DeserializeObject<ChatCompletion>(response.Content);
                    if (isThink)
                    {
                        return Ok(chatCompletion);
                    }
                    else 
                    {
                        foreach (var choice in chatCompletion.Choices)
                        {
                            choice.message.content = Regex.Replace(choice.message.content, "<think>.*?</think>", string.Empty, RegexOptions.Singleline);
                            choice.message.content = choice.message.content.Replace("```json", "").Replace("```", "");

                            ToolCall tool = new ToolCall();
                            tool.function = JsonConvert.DeserializeObject<FunctionCallBridge.OpenAIModel.Function>(choice.message.content);
                            tool.function.arguments = JsonConvert.SerializeObject(tool.function.arguments);
                            choice.message.tool_calls = new List<ToolCall>() {
                                tool
                            };


                            choice.message.content = null;
                            if (isFc)
                            {
                                choice.finish_reason = "tool_calls";
                            }
                        }

                        Console.WriteLine("响应:");
                        Console.WriteLine(JsonConvert.SerializeObject(chatCompletion, Formatting.Indented));
                        return Ok(chatCompletion);
                    }
                }
                else
                {
                    return StatusCode((int)response.StatusCode, response.Content);
                }
            }
        }

        private void BuildFunctionCallPrompt(OpenAIRequest model)
        {
            string fcPrompt = System.IO.File.ReadAllText("FunctionCallPrompt.txt");

            var functions = new StringBuilder();
            foreach (var tool in model.tools)
            {
                functions.AppendLine($"- {tool.function.name}: {tool.function.description}");
            }
            fcPrompt = fcPrompt.Replace("$(functions)", functions.ToString());
            fcPrompt = fcPrompt.Replace("$(input)", model.messages[model.messages.Count-1].content);


            model.messages[model.messages.Count - 1].content = fcPrompt;

            model.tools = null;
        }

        private void BuildJsonPrompt(OpenAIRequest model)
        {
            string jsonPrompt = System.IO.File.ReadAllText("JsonPrompt.txt");
            jsonPrompt = jsonPrompt.Replace("$(input)", model.messages[model.messages.Count - 1].content);
            model.messages[model.messages.Count - 1].content = jsonPrompt;
        }  
    }
}
