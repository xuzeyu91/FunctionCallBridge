# FunctionCallBridge 是一款让不支持function call的模型通过提示词方式支持兼容function call格式的桥接插件。

把原有ollama 接口http://localhost:11434/v1/chat/completions 替换为调用

FunctionCallBridge 的http://localhost:5000/v1/chat/completions

即可实现 json_object 和function call的能力。提示词可根据模型情况调整
