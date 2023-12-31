
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Renci.SshNet;

namespace KosmaPanel
{
    class WebServer
    {

        public void Start(string d_port, string d_host)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    int port = int.Parse(d_port);
                    options.Listen(IPAddress.Parse(d_host), port);
                })
                .Configure(ConfigureApp)
                .Build();
            host.Run();
        }
        private static void ConfigureApp(IApplicationBuilder app)
        {
            app.Run(ProcessRequest);
        }
        private static async Task ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var (isValidKey, keyMessage) = IsAuthorized(request);
            if (isValidKey)
            {
                var absolutePath = request.Path.Value.TrimStart('/');
                switch (absolutePath)
                {
                    case "":
                        {
                            var errorResponse = new
                            {
                                message = "Bad Request",
                                error = "Please provide a valid API endpoint."
                            };
                            var errorJson = JsonConvert.SerializeObject(errorResponse);
                            var errorBuffer = Encoding.UTF8.GetBytes(errorJson);
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                            response.ContentType = "application/json";
                            response.ContentLength = errorBuffer.Length;
                            await response.Body.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                            break;
                        }
                    case "test":
                        {
                            var presponse = new
                            {
                                message = "Example Request",
                                error = "This is an example request"
                            };
                            var pjson = JsonConvert.SerializeObject(presponse);
                            var pBuffer = Encoding.UTF8.GetBytes(pjson);
                            response.StatusCode = (int)HttpStatusCode.OK;
                            response.ContentType = "application/json";
                            response.ContentLength = pBuffer.Length;
                            await response.Body.WriteAsync(pBuffer, 0, pBuffer.Length);
                            break;
                        }
                    case "execute":
                        {
                            string command = request.Query["command"];
                            try
                            {
                                var sshHost = ConfigManager.GetSetting("Daemon", "ssh_ip");
                                var sshUsername = ConfigManager.GetSetting("Daemon", "ssh_username");
                                var sshPassword = ConfigManager.GetSetting("Daemon", "ssh_password");
                                var sshPort = int.Parse(ConfigManager.GetSetting("Daemon", "ssh_port"));
                                try
                                {
                                    using var client = new SshClient(sshHost, sshPort, sshUsername, sshPassword);
                                    client.Connect();
                                    var sshCommand = client.CreateCommand(command);
                                    var result = sshCommand.Execute();
                                    client.Disconnect();

                                    var executeResponse = new
                                    {
                                        message = "Command Executed",
                                        result = result
                                    };

                                    var executeJson = JsonConvert.SerializeObject(executeResponse);
                                    var executeBuffer = Encoding.UTF8.GetBytes(executeJson);
                                    response.StatusCode = (int)HttpStatusCode.OK;
                                    response.ContentType = "application/json";
                                    response.ContentLength = executeBuffer.Length;
                                    await response.Body.WriteAsync(executeBuffer, 0, executeBuffer.Length);
                                }
                                catch (Exception ex)
                                {
                                    Program.logger.Log(LogType.Error, $"[WebServer] {ex.Message}");
                                    var errorResponse = new
                                    {
                                        message = "I'm sorry, but I can't reach the web manager ssh client!",
                                        error = ex.Message
                                    };

                                    var errorJson = JsonConvert.SerializeObject(errorResponse);
                                    var errorBuffer = Encoding.UTF8.GetBytes(errorJson);
                                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                    response.ContentType = "application/json";
                                    response.ContentLength = errorBuffer.Length;
                                    await response.Body.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                                }
                            }
                            catch (Exception ex)
                            {
                                Program.logger.Log(LogType.Error, $"[WebServer] {ex.Message}"); var errorResponse = new
                                {
                                    message = "I'm sorry, but some unexpected error got thrown out, and I don't know how to handle it. Please contact support.",
                                    error = ex.Message
                                };

                                var errorJson = JsonConvert.SerializeObject(errorResponse);
                                var errorBuffer = Encoding.UTF8.GetBytes(errorJson);
                                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                response.ContentType = "application/json";
                                response.ContentLength = errorBuffer.Length;
                                await response.Body.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                            }

                            break;
                        }
                    default:
                        {
                            var errorResponse = new
                            {
                                message = "Page not found",
                                error = "The requested page does not exist."
                            };
                            var errorJson = JsonConvert.SerializeObject(errorResponse);
                            var errorBuffer = Encoding.UTF8.GetBytes(errorJson);
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            response.ContentType = "application/json";
                            response.ContentLength = errorBuffer.Length;
                            await response.Body.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                            break;
                        }
                }
            }
            else
            {
                var errorResponse = new
                {
                    message = keyMessage,
                    error = "Invalid API key."
                };
                var errorJson = JsonConvert.SerializeObject(errorResponse);
                var errorBuffer = Encoding.UTF8.GetBytes(errorJson);
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.ContentType = "application/json";
                response.ContentLength = errorBuffer.Length;
                await response.Body.WriteAsync(errorBuffer, 0, errorBuffer.Length);
            }
        }
        private static (bool isValid, string message) IsAuthorized(HttpRequest request)
        {
            string apiKey = request.Headers["Authorization"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return (false, "API key is empty.");
            }

            if (apiKey == ConfigManager.GetSetting("Daemon", "key"))
            {
                return (true, "Authorized.");
            }

            return (false, "API key is invalid.");
        }
    }
}