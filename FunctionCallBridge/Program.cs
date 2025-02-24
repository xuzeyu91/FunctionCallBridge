using FunctionCallBridge.OpenAIModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Configuration.GetSection("AIOption").Get<AIOption>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
Console.WriteLine("FunctionCallBridge 是一个针对deepseek-r1 小参数进行函数调用 与json格式转换的桥接工具，联系作者微信:xuzeyu91");
Console.WriteLine();
app.Run();


