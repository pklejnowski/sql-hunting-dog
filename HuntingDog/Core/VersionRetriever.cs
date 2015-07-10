﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Reflection;

namespace HuntingDog.Core
{
    public class VersionRetrieverResult
    {
        public VersionRetrieverResult(bool isRetrieved, DogVersion version)
        {
            IsRetrieved = isRetrieved;
            RetrievedVersion = version;
        }
        public bool IsRetrieved { get; private set; }
        public DogVersion RetrievedVersion { get; private set; }
    }

    public class VersionRetriever
    {
        private static readonly Log log = LogFactory.GetLog();   

        public VersionRetrieverResult RetrieveVersion(string url)
        {
            try
            {

                string content = string.Empty;
                using (WebClient client = new WebClient())
                {
                    using (Stream stream = client.OpenRead(url))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            var version =  ParseVersion(reader.ReadToEnd());
                            return new VersionRetrieverResult(version != null, version);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Unable to retrive version", ex);
            }

            return new VersionRetrieverResult(false,null);
        }

        private DogVersion ParseVersion(string content)
        {
            try
            {
                var lines  = content.Split(new string[]{ "\r\n" }, StringSplitOptions.None);
                if (lines.Length >= 2)
                {
                    var digits = lines[0].Split('.');
                    int major = int.Parse(digits[0]);
                    int minor = int.Parse(digits[1]);

                    var url = lines[1];
                    return new DogVersion(new Version(major,minor) ,url);
                }              
            }
            catch (Exception ex)
            {
                log.Error("Received corrupter version - unable to parse ", ex);
            }
            return null;
        }
    }
}
