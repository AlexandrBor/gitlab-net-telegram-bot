using System;

namespace GitlabTelegramBot.Options
{
    public class ProxiesConfig
    {
        public ProxyConfig[] Proxies { get; set; }

        public ProxiesConfig()
        {
            Proxies = new ProxyConfig[0];
        }
    }

    public class ProxyConfig
    {
        public Boolean Enabled {get;set;}
        public String Host {get;set;}
        public Int32 Port {get;set;}
        public String UserName {get;set;}
        public String Password {get;set;}

        public override string ToString()
        {
            return Enabled
                ? $"{Host}:{Port} - Enabled:{Enabled}"
                : "-none-";
        }
    }
}