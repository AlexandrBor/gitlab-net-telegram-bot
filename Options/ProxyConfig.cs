using System;

namespace GitlabTelegramBot.Options
{
    public class ProxyConfig
    {
        public Boolean Enabled {get;set;}
        public String Host {get;set;}
        public Int32 Port {get;set;}
        public String UserName {get;set;}
        public String Password {get;set;}
    }
}