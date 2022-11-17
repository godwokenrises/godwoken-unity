

using System.Collections.Generic;

namespace Godwoken
{
    [System.Serializable]
    public class RootMetadata
    {
        public List<ExampleNFTMetadata> tokens;
    }
    
    [System.Serializable]
    public class ExampleNFTMetadata
    {
        public int id;

        public string description;

        public string external_url;

        public string external_url_more_info;

        public string image;

        public string name;
    }
}