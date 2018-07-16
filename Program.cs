using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace WebRequestWrapper
{
    class Program
    {
        private static void InitServerCertificateValid()
        {
            ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallback;
        }

        private static bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var result = true;

            // if there are errors in the certificate chain,
            // look at each error to determine the cause
            if (SslPolicyErrors.None != sslPolicyErrors)
            {
                for (int index = 0; index < chain.ChainStatus.Length; ++index)
                {
                    var state = chain.ChainStatus[index];
                    if (X509ChainStatusFlags.RevocationStatusUnknown != state.Status)
                    {
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                        bool isChainValid = chain.Build((X509Certificate2)certificate);
                        if (!isChainValid)
                        {
                            result = false;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private static void DownloadWebFile(string url, string destPath, Action onComplete)
        {
            var directoryName = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }

            HttpWebRequest request = null;
            HttpWebResponse response = null;
            bool success = true;
            try
            {
                do
                {
                    InitServerCertificateValid();
                    request = (HttpWebRequest)WebRequest.Create(url);
                    if (null == request)
                    {
                        success = false;
                        break;
                    }
                    request.Timeout = 5000;

                    response = request.GetResponse() as HttpWebResponse;
                    if (null == response)
                    {
                        success = false;
                        break;
                    }

                    if (HttpStatusCode.OK != response.StatusCode)
                    {
                        success = false;
                        break;
                    }

                } while (false);
            }
            catch (Exception ex)
            {
                success = false;
            }

            if (success)
            {
                using (var stream = response.GetResponseStream())
                {
                    using (FileStream fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] datas = new byte[2 * 1024 * 1024];
                        int readCount = 0;
                        double readTotal = 0;
                        double fileSize = response.ContentLength;
                        while (true)
                        {
                            readCount = stream.Read(datas, 0, datas.Length);
                            if (readCount <= 0)
                            {
                                break;
                            }
                            readTotal += readCount;
                            fileStream.Write(datas, 0, readCount);
                            fileStream.Flush();

                            float progress = (float)(readTotal / fileSize);
                        }
                        onComplete?.Invoke();
                    }
                }
            }

            if (null != response)
            {
                response.Close();
                response = null;
            }

            if (null != request)
            {
                request.Abort();
                request = null;
            }
        }


        static void Main(string[] args)
        {
            DownloadWebFile("http://192.168.1.94/Download/test.apk", "F:/data/Test.apk", null);
            Console.ReadLine();
        }
    }
}
