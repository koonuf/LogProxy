using System;
using System.Configuration;
using LogProxy.Lib;
using LogProxy.Lib.Inspection.Implementation;
using LogProxy.Lib.Sockets;

namespace LogProxy.Console
{
    class Program
    {
        private const string ListenPortSettingName = "listenPort";
        private const string MessageListLogFileSettingName = "messageListLogFile";
        private const string MessageBodyLogDirSettingName = "messageBodyLogDir";
        private const string LogMessageBodySettingName = "logMessageBody";
        private const string DnsCacheSettingName = "dnsCache";
        private const string InactivityLogTimeInMinutesSettingName = "inactivityLogTimeInMinutes";

        static void Main(string[] args)
        {
            string dnsCacheFile = GetSettingValue(DnsCacheSettingName, defaultValue: null);
            if (!string.IsNullOrEmpty(dnsCacheFile))
            {
                DnsCacheReader.ReadDnsCache(dnsCacheFile, HostConnector.DnsCache);
            }

            var settings = ConstructSettings();
            var listener = new TcpListener(settings);
            listener.ListenToNewConnections();
        }

        private static ProxySettings ConstructSettings()
        {
            string logFileName = GetSettingValue(MessageListLogFileSettingName, "C:\\messages.txt");
            int inactivityTimeMinutes = GetSettingValue(InactivityLogTimeInMinutesSettingName, 3);

            var settings = new ProxySettings 
            { 
                ListenPort = GetSettingValue(ListenPortSettingName, defaultValue: 5555),
                LogMessageBody = GetSettingValue(LogMessageBodySettingName, defaultValue: true),
                MessageBodyLogDirectory = GetSettingValue(MessageBodyLogDirSettingName, defaultValue: "C:\\Logs"),
                CertificateProvider = new MakeCertWrapper.CertificateProvider(@"C:\Program Files (x86)\Fiddler2\makecert.exe"),
            };

            settings.InspectorFactory = new InspectorFactory(settings);

            return settings;
        }

        private static string GetSettingValue(string settingName, string defaultValue)
        {
            return ConfigurationManager.AppSettings[settingName] ?? defaultValue;
        }

        private static int GetSettingValue(string settingName, int defaultValue)
        {
            string value = ConfigurationManager.AppSettings[settingName];
            if (value == null)
            {
                return defaultValue;
            }
            else
            {
                return int.Parse(value);
            }
        }

        private static bool GetSettingValue(string settingName, bool defaultValue)
        {
            string value = ConfigurationManager.AppSettings[settingName];
            if (value == null)
            {
                return defaultValue;
            }
            else
            {
                return bool.Parse(value);
            }
        }
    }
}
