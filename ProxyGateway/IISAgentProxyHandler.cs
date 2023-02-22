using System;
using System.Web;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Net.WebSockets;
using System.Web.WebSockets;
using System.Text;
using System.Configuration;
using Newtonsoft.Json;

namespace ProxyGateway
{
    public class IISAgentProxyHandler : IHttpHandler
    {

        //private static Dictionary<String,Dictionary<string,Command>> ToBeProcessed = new Dictionary<string, Dictionary<string, Command>>();
        private static Dictionary<string,Command> agentSessions = new Dictionary<string, Command>();
        private static Dictionary<string, Command> commandSessions = new Dictionary<string, Command>();
        //private static Dictionary<String, Dictionary<string, Command>> Processed = new Dictionary<string, Dictionary<string, Command>>();
        /// <summary>
        /// You will need to configure this handler in the Web.config file of your 
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: https://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpHandler Members

        public bool IsReusable
        {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
            {
                context.AcceptWebSocketRequest(this.ProcessWebsocketRequest);
            } else
            {
                var reqst = context.Request;
                var resp = context.Response;
                this.processHttpRequest(reqst, resp);
            }
        }

        private void processHttpRequest(HttpRequest reqst, HttpResponse resp)
        {
            resp.Headers["Access-Control-Allow-Origin"] = "*";
            var path = reqst.Path;

            var parameters = new Dictionary<string, string>();

            if (reqst.QueryString != null && reqst.QueryString.Count > 0)
            {
                foreach (var key in reqst.QueryString.AllKeys)
                {
                    var val = reqst.QueryString[key];
                    parameters[key] = val;
                }
            }
            if (reqst.Form.Count>0)
            {
                foreach (var key in reqst.Form.AllKeys)
                {
                    var val = reqst.Form[key];
                    parameters[key] = val;
                }
            }
            /*if (path != "" && path.Contains("/agentapi/"))
            {
                var command = path.Substring(path.LastIndexOf("/agentapi/") + "/agentapi/".Length);
                if (command != "")
                {
                    var agentid = "";
                    var cmd = new Command();
                    cmd.Cmd = command;
                    foreach (var key in parameters.Keys)
                    {
                        var val = parameters[key];
                        if (val != "")
                        {
                            if (key.Equals("agentid", StringComparison.OrdinalIgnoreCase) || key.Equals("login", StringComparison.OrdinalIgnoreCase))
                            {
                                agentid = val;

                            }
                            else
                            {
                                cmd.Arguments[key] = val;
                            }
                        }
                    }
                    if (agentid != "")
                    {
                        cmd.Ready = true;
                        (!ToBeProcessed.ContainsKey(agentid) ? (ToBeProcessed[agentid] = new Dictionary<string, Command>()) : ToBeProcessed[agentid])[command] = cmd;
                        var count = 10;
                        while (count > 0 && ToBeProcessed[agentid].ContainsKey(command))
                        {
                            Thread.Sleep(1000);
                            count--;
                        }
                    }
                }
            }
            else if (path != "" && path.Contains("/agentapi-ready/"))
            {
                resp.Headers["Content-Type"] = "application/json";
                var command = path.Substring(path.LastIndexOf("/agentapi-ready/") + "/agentapi-ready/".Length);
                if (command != "")
                {
                    var agentid = "";
                    foreach (var key in parameters.Keys)
                    {
                        var val = parameters[key];
                        if (val != "")
                        {
                            if (key.Equals("agentid", StringComparison.OrdinalIgnoreCase))
                            {
                                agentid = val;
                                break;
                            }
                        }
                    }
                    if (agentid != "")
                    {
                        if (ToBeProcessed.ContainsKey(agentid))
                        {
                            if (ToBeProcessed[agentid].ContainsKey(command))
                            {
                                var cmd = ToBeProcessed[agentid][command];
                                resp.Write("{");
                                resp.Write("\"command\":\"" + cmd.Cmd + "\",");
                                foreach (var key in cmd.Arguments.Keys)
                                {
                                    resp.Write("\"" + key + "\":\"" + cmd.Arguments[key] + "\",");
                                }
                                resp.Write("\"Ready\":true}");
                            }
                            else
                            {
                                resp.Write("{}");
                            }
                        }
                        else
                        {
                            resp.Write("{}");
                        }
                    }
                    else
                    {
                        resp.Write("{}");
                    }
                }
            }
            else if (path != "" && path.Contains("/agentapi-process/"))
            {
                var command = path.Substring(path.LastIndexOf("/agentapi-process/") + "/agentapi-process/".Length);
                if (command != "")
                {
                    var agentid = "";
                    foreach (var key in parameters.Keys)
                    {
                        var val = parameters[key];
                        if (val != "")
                        {
                            if (key.Equals("agentid", StringComparison.OrdinalIgnoreCase))
                            {
                                agentid = val;
                                break;
                            }
                        }
                    }
                    if (agentid != "")
                    {
                        if (ToBeProcessed.ContainsKey(agentid))
                        {
                            if (ToBeProcessed[agentid].ContainsKey(command))
                            {
                                var cmd = ToBeProcessed[agentid][command];
                                ToBeProcessed[agentid][command] = null;
                                ToBeProcessed[agentid].Remove(command);
                                cmd.Ready = false;
                            }
                        }
                    }
                }
            }*/
            if (path != "" && path.Contains("/agentapi-map/"))
            {
                resp.ContentType = "application/json";
                resp.Write("{");
                var cnt = agentSessions.Count;
                foreach (var kvalset in agentSessions)
                {
                    cnt--;
                    resp.Write("\""+kvalset.Key+"\":"+"\""+kvalset.Value.Sessionid+"\"");
                    if (cnt > 0)
                    {
                        resp.Write(",");
                    }
                }
                resp.Write("}");
            } else if (path != "" && path.Contains("/agentapi/"))
            {
                var agentid = "";
                var sessionid = "";
                var reqcommand = path.Substring(path.LastIndexOf("/agentapi/") + "/agentapi/".Length);
                var args = new Dictionary<string, string>();
                foreach (var key in parameters.Keys)
                {
                    var val = parameters[key];
                    if (val != "")
                    {
                        if (key.Equals("session", StringComparison.OrdinalIgnoreCase) || key.Equals("sessionid", StringComparison.OrdinalIgnoreCase))
                        {
                            sessionid = val;
                        }
                        else if (key.Equals("agentid", StringComparison.OrdinalIgnoreCase) || key.Equals("login", StringComparison.OrdinalIgnoreCase))
                        {
                            agentid = val;
                        } else
                        {
                            args[key] = val;
                        }
                    }
                }

                if (reqcommand != "")
                {
                    Command cmd = agentid != "" ? agentSessions.ContainsKey(agentid) ? agentSessions[agentid] : sessionid != "" ? commandSessions.ContainsKey(sessionid) ? commandSessions[sessionid] : null : null : null;
                    if (cmd != null)
                    {
                        cmd.ExecuteRequest(resp,reqst, args, reqcommand);
                    }
                }
            }
            else if (path != "" && path.Contains("/agentapi-session/"))
            {
                var agentid = "";
                var sessionid = "";
                foreach (var key in parameters.Keys)
                {
                    var val = parameters[key];

                    if (val != "")
                    {
                        if (key.Equals("session", StringComparison.OrdinalIgnoreCase) || key.Equals("sessionid", StringComparison.OrdinalIgnoreCase))
                        {
                            sessionid = val;
                        }
                        else if (key.Equals("agentid", StringComparison.OrdinalIgnoreCase) || key.Equals("login", StringComparison.OrdinalIgnoreCase))
                        {
                            agentid = val;
                        }
                    }
                }

                if (sessionid != "" && agentid=="")
                {
                    Command cmd = new Command();
                    cmd.Sessionid = sessionid;
                }

                if (agentid != "")
                {
                    Command cmd = null;
                    if (!agentSessions.ContainsKey(agentid))
                    {
                        cmd = new Command();
                        agentSessions[agentid] = cmd;
                        cmd.Agentid = agentid;
                        cmd.RemoteHost = reqst.UserHostAddress;
                    } else
                    {
                        cmd = agentSessions[agentid];
                        cmd.RemoteHost = reqst.UserHostAddress;
                    }

                    if (sessionid != "")
                    {
                        if (cmd != null && !commandSessions.ContainsKey(sessionid))
                        {
                            cmd.Sessionid = sessionid;
                            commandSessions[sessionid] = cmd;
                        }
                        else
                        {
                            cmd = commandSessions[sessionid];
                            cmd.Sessionid = sessionid;
                        }
                    }
                }
                if (path.EndsWith("/session.html"))
                {
                    resp.ContentType = "text/html";

                    using (var sessionhtml = new StreamReader(reqst.PhysicalApplicationPath + "session.html"))
                    {
                        resp.Write(sessionhtml.ReadToEnd());
                    }
                }
                if (path.EndsWith("/sessionidcookie.js"))
                {
                    resp.ContentType = "text/javascript";

                    using (var sessionhtml = new StreamReader(reqst.PhysicalApplicationPath + "sessionidcookie.js"))
                    {
                        resp.Write(sessionhtml.ReadToEnd());
                    }
                }
            }
            try
            {
                resp.Flush();
            }
            catch (Exception e) { }
        }

        public async Task ProcessWebsocketRequest(AspNetWebSocketContext wsContext)
        {
            var wshandler = new WebsocketHandler();
            WebSocketMessageType lastMessageType = WebSocketMessageType.Text;
            while (wsContext.WebSocket.State == WebSocketState.Open)
            {
                if (wshandler.Length > 0)
                {
                    long currentLen = wshandler.Length;
                    long writtenLen = 0;
                    byte[] writeBytes = new Byte[8192];

                    int lastRlen = 0;
                    while (writtenLen < currentLen)
                    {
                        while ((lastRlen = await wshandler.ReadAsync(writeBytes, 0, writeBytes.Length)) > 0) {
                            ArraySegment<byte> writeBuffer = new ArraySegment<byte>(writeBytes, 0,lastRlen);
                            while (writeBuffer.Offset < writeBuffer.Count)
                            {
                                writtenLen += writeBuffer.Count - writeBuffer.Count;
                                await wsContext.WebSocket.SendAsync(writeBuffer, lastMessageType, writtenLen == currentLen, CancellationToken.None);
                            }
                        }
                    }
                }

                if (wshandler.CanClose)
                {
                    await wsContext.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }

                WebSocketReceiveResult resultrecv = null;
                Byte[] readBytes = new Byte[8192];
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        ArraySegment<byte> readBuffer = new ArraySegment<byte>(readBytes);
                        resultrecv = await wsContext.WebSocket.ReceiveAsync(readBuffer, CancellationToken.None);
                        await ms.WriteAsync(readBuffer.Array, readBuffer.Offset, readBuffer.Count);
                    } while (!resultrecv.EndOfMessage);
                    if (resultrecv.MessageType == WebSocketMessageType.Text)
                    {
                        if (lastMessageType!=resultrecv.MessageType)
                        {
                            lastMessageType = resultrecv.MessageType;
                        }
                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            await wshandler.OnReadAsync(wsContext.Path, await reader.ReadToEndAsync());
                        }
                    } else if (resultrecv.MessageType==WebSocketMessageType.Binary)
                    {
                        using(var reader = new BinaryReader(ms, Encoding.UTF8, true))
                        {
                           await wshandler.OnBinaryReadAsync(wsContext.Path,reader);
                        }
                    }
                }
            }
        }

        #endregion
    }

    class Command
    {
        internal string RemoteHost;
        internal string Sessionid;
        internal string Agentid;
        internal Dictionary<string, string> Arguments;
        internal bool Ready;

        internal Command() {
            Arguments = new Dictionary<string, string>();
        }

        internal void ExecuteRequest(HttpResponse resp,HttpRequest reqst, Dictionary<string, string> args, string reqcommand)
        {

            var urlpath = "";
            System.Net.WebRequest webRequest = null;
            if (this.Sessionid != "")
            {
                urlpath = ConfigurationManager.AppSettings["webagenturl"];
                if (urlpath != "")
                {
                    if (!urlpath.EndsWith("/"))
                    {
                        urlpath += "/";
                    }
                    urlpath += "Integration.aspx?operation=" + reqcommand + "&SessionId=" + this.Sessionid;
                }
            }
            else if (this.RemoteHost != "")
            {

                urlpath = "http://" + this.RemoteHost + "/" + reqcommand;
            }

            if (urlpath != "")
            {
                if (args.Count>0)
                {
                    foreach(var keyval in args)
                    {
                        urlpath += "&" + keyval.Key + "=" + keyval.Value;
                    }
                }
                Uri uri = new Uri(urlpath);
                webRequest = System.Net.HttpWebRequest.Create(uri);
            }

            if (webRequest != null)
            {
                resp.ContentType = "application/xml";
                try
                {
                    System.Net.WebResponse webResponse = webRequest.GetResponse();
                    StreamReader streamReader = new StreamReader(webResponse.GetResponseStream());
                    resp.ContentType = "application/xml";
                    resp.Write(streamReader.ReadToEnd());
                } catch(Exception e)
                {
                    resp.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" ?><AgentHTTPResponse><Request>" + reqcommand + @"</Request><Result>$-1</Result><Parameters></Parameters></AgentHTTPResponse>");
                }
            }
        }
    }

    class WebsocketHandler
    {
        MemoryStream memStream = null;
        internal StreamWriter Writer = null;

        internal bool CanClose = false;

        internal WebsocketHandler() {
            this.memStream = new MemoryStream();
            this.Writer = new StreamWriter(memStream, Encoding.UTF8);
            this.Writer.AutoFlush = true;
        }

        internal async Task OnBinaryReadAsync(string path, BinaryReader reader)
        {
            
        }

        internal async Task OnReadAsync(string path,string contentRead)
        {
            
        }


        internal async Task<int> ReadAsync(byte[] b,int offset,int count)
        {
            return await this.memStream.ReadAsync(b, offset, count);
        }

        internal long Length
        {
            get { return this.memStream.Length; }
        }
    }

}
