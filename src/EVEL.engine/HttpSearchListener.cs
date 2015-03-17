using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;
using System.Threading;
using Evel.interfaces;

namespace Evel.engine.net {

    public class HttpSearchListener {

        public event EventHandler OnEvent;

        private HttpListener xmlListener;

        private Dictionary<string, IProject> _projects;

        void listenForRequests() {
            while (xmlListener.IsListening) {
            //lock (xmlListener) {
                IAsyncResult result = xmlListener.BeginGetContext(new AsyncCallback(listenForRequests), xmlListener);
                result.AsyncWaitHandle.WaitOne();
            }
            //xmlListener.Stop();
        }

        public void listenForRequests(IAsyncResult result) {
            //Stream output = null;
            ////XmlDocument document = null;
            ////XmlDocument responseDoc = null;
            //XmlWriter responseWriter;
            //XmlReader requestReader;
            //HttpListenerRequest request = null;
            //HttpListenerResponse response = null;
            //string requestContentType = "";
            //try {
            //    HttpListener listener = (HttpListener)result.AsyncState;
            //    if (!listener.IsListening) {
            //        Console.WriteLine("Stopping server");
            //        return;
            //    }
            //    //} else
            //    //    Console.WriteLine("Processing request...");
            //    HttpListenerContext context = listener.EndGetContext(result);
            //    //Console.WriteLine(ClientInformation(context));
            //    request = context.Request;
            //    requestContentType = request.ContentType;
            //    response = context.Response;
            //    response.ContentType = "text/xml";
            //    output = response.OutputStream;
            //    //responseDoc = new XmlDocument();
            //    responseWriter = XmlWriter.Create(output);
            //    //responseDoc.AppendChild(responseDoc.CreateXmlDeclaration("1.0", null, null));
            //    //XmlElement root = responseDoc.CreateElement("response");
            //    responseWriter.WriteStartElement("response");
            //    if (request.ContentType == "text/xml") {
            //        //XmlDocument requestDoc = new XmlDocument();
            //        XmlReaderSettings readerSettings = new XmlReaderSettings();
            //        readerSettings.IgnoreWhitespace = true;
            //        requestReader = XmlReader.Create(request.InputStream, readerSettings);
            //        requestReader.ReadToFollowing("request");
            //        //requestDoc.Load(request.InputStream);
            //        string modelClassName = "";
            //        string projectClassName = "";
            //        while (requestReader.MoveToNextAttribute()) {
            //            switch (requestReader.Name) {
            //                case "modelclass": modelClassName = requestReader.Value; break;
            //                case "projectclass": projectClassName = requestReader.Value; break;
            //            }
            //        }
            //        requestReader.MoveToElement();
            //        IModel model = AvailableAssemblies.getModel(modelClassName);
            //        IProject project = null;
            //        ISpectraContainer container;
            //        //if (_projects[projectClassName] != null) {
            //        if (_projects.TryGetValue(projectClassName, out project)) {
            //            project = _projects[projectClassName];
            //            project.Containers.Clear();
            //            container = project.CreateContainer(model);
            //            project.AddSpectraContainer(container);
            //            //container = ((List<ISpectraContainer>)project.Containers)[0];
            //            //container.Spectra.Clear();
            //        } else {
            //            project = AvailableAssemblies.getProject(projectClassName, new object[] { });
            //            _projects.Add(projectClassName, project);
            //            container = project.CreateContainer(model);
            //            project.AddSpectraContainer(container);
            //        }
            //        requestReader.ReadToFollowing("Searchflags");
            //        project.Flags.Clear();
            //        while (requestReader.MoveToNextAttribute()) {
            //            project.Flags.Add(requestReader.Name, Boolean.Parse(requestReader.Value));
            //        }
            //        requestReader.ReadToFollowing("package");
            //        do {
            //            requestReader.ReadToDescendant("spectrum");
            //            XmlReader spectrumReader = requestReader.ReadSubtree();
            //            ISpectrum spectrum = container.CreateSpectrum(spectrumReader);
            //            container.Spectra.Add(spectrum);
            //            spectrumReader.Close();
            //            double[] diffs;
            //            //Search
            //            int ipi;
            //            project.Search(container, spectrum, out diffs, out ipi);
            //            responseWriter.WriteStartElement("package");
            //            responseWriter.WriteAttributeString("fit", spectrum.Fit.ToString());
            //            responseWriter.WriteAttributeString("ipi", ipi.ToString());
            //            //spectrum
            //            spectrum.writeToXml(responseWriter, false);
            //            //cache
            //            requestReader.ReadToNextSibling("cache");
            //            responseWriter.WriteNode(requestReader, true);
            //            //diffs
            //            responseWriter.WriteStartElement("differences");
            //            foreach (double value in diffs) {
            //                responseWriter.WriteValue(value);
            //                responseWriter.WriteCharEntity('\t');
            //            }
            //            //StringBuilder builder = new StringBuilder("");
            //            //for (int i = 0; i < diffs.Length; i++) {
            //                //responseWriter.WriteValue(
            //                //builder.Append(" ");
            //                //builder.Append(diffs[i]);
            //            //}
            //            //responseWriter.WriteString(builder.ToString());
            //            responseWriter.WriteEndElement(); //differences

            //            responseWriter.WriteEndElement(); //package
            //        } while (requestReader.ReadToNextSibling("package"));
            //        responseWriter.WriteEndElement(); //response
            //        responseWriter.Flush();
            //        responseWriter.Close();
            //        //requestReader.Close();
            //    } else {
            //        throw new Exception("NOT A XML REQUEST");
            //    }
            //} catch (Exception e) {
            //    try {
            //        if (requestContentType == "text/xml") {
            //            responseWriter = XmlWriter.Create(output);
            //            responseWriter.WriteStartElement("response");
            //            responseWriter.WriteStartElement("error");
            //            responseWriter.WriteString(e.Message);
            //            responseWriter.WriteEndElement(); //error
            //            responseWriter.WriteEndElement(); //response
            //            responseWriter.Flush();
            //            responseWriter.Close();
            //        } else {
            //            response.ContentType = "text/html";
            //            string responseString = "<HTML><HEAD><TITLE>EVEL LISTENER</TITLE><style>body { font-family: verdana; color: #361D77; } h1 { margin-bottom: 0px; padding-bottom: 0px; } h3 { font-weight: normal; margin-top: 0px; padding-top: 0px; } </style></HEAD>" +
            //                "<BODY><h1>EVEL</h1><h3><strong>EV</strong>aluations of <strong>E</strong>xperimenta<strong>L</strong> data</h3>Only xml documents containing spectra parameters are accepted as requests.</BODY></HTML>";
            //            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            //            // Get a response stream and write the response to it.
            //            response.ContentLength64 = buffer.Length;
            //            output.Write(buffer, 0, buffer.Length);
            //            // You must close the output stream.
            //            //output.Close();
            //        }
            //    } catch { }
            //} finally {
            //    if (output != null)
            //        try {
            //            output.Close();
            //        } catch { }
            //}
            
            
            //xmlListener.Stop();

            /*HttpListener listener = (HttpListener)result.AsyncState;
            // Call EndGetContext to complete the asynchronous operation.
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            // Obtain a response object.
            HttpListenerResponse response = context.Response;
            // Construct a response.
            string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();*/

        }

        public string ClientInformation(HttpListenerContext context)
        {
            /*System.Security.Principal.IPrincipal user = context.User;
            
            System.Security.Principal.IIdentity id = user.Identity;
            if (id == null)
            {
                return "Client authentication is not enabled for this Web server.";
            }

            string display;
            if (id.IsAuthenticated)
            {
                display = String.Format("{0} was authenticated using {1}", id.Name,
                    id.AuthenticationType);
            }
            else
            {
                display = String.Format("{0} was not authenticated", id.Name);
            }
            return display;*/
            return "";
        }

        public HttpSearchListener(string[] prefixes, EventHandler eventHandler) {
            try {
                this.OnEvent = eventHandler;
                xmlListener = new HttpListener();
                xmlListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                foreach (string prefix in prefixes) {
                    xmlListener.Prefixes.Add(prefix);
                //xmlListener.Prefixes.Add("http://localhost:7898/getxml/");
                //xmlListener.Prefixes.Add("http://*:7898/getxml/");
                //xmlListener.Prefixes.Add("http://esprit:7898/getxml/");
                //xmlListener.Prefixes.Add("http://+:7898/getxml/");
                //xmlListener.Prefixes.Add("http://192.168.0.11:7898/getxml/");
                }
                //xmlListener.Start();
                //IAsyncResult result = xmlListener.BeginGetContext(new AsyncCallback(listenForRequests), xmlListener);
                //Thread serverThread = new Thread(listenForRequests);
                //serverThread.Start();
                Start();
                _projects = new Dictionary<string,IProject>();
                //Console.WriteLine("Server started. Waiting for request...");
                //Console.WriteLine("Press enter to exit server");
                //Console.Read();
                //xmlListener.Stop();
            } catch (HttpListenerException e) {
                //Console.WriteLine(String.Format("Error code: {0}\nError message: {1}", e.ErrorCode, e.Message));
                //Console.Read();
                if (OnEvent!= null)
                    OnEvent(e, null);
            }
        }

        public void Stop() {
            if (xmlListener != null)
                xmlListener.Stop();
        }

        public void Start() {
                xmlListener.Start();
                //IAsyncResult result = xmlListener.BeginGetContext(new AsyncCallback(listenForRequests), xmlListener);
                Thread serverThread = new Thread(listenForRequests);
                serverThread.Start();
        }

        public bool IsListening {
            get { return this.xmlListener.IsListening; }
        }

    }
}
