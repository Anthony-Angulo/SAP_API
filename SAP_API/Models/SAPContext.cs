using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace SAP_API.Models
{
    public class SAPContext
    {

        public SAPbobsCOM.Company oCompany;

        public SAPContext()
        {
            oCompany = new SAPbobsCOM.Company();
            oCompany.Server = "192.168.0.92:30015";
            oCompany.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_HANADB;
            //oCompany.CompanyDB = "CCFN_PRODUCCCION";
            oCompany.CompanyDB = "CCFN_PROD";
            oCompany.UserName = "SISTEMAS04";
            oCompany.Password = "SAP1234";
            oCompany.DbUserName = "SYSTEM";
            oCompany.DbPassword = "B1AdminH2";
            oCompany.LicenseServer = "192.168.0.92:40000";
            oCompany.language = SAPbobsCOM.BoSuppLangs.ln_English;
        }

        public SAPContext GetConnection()
        {
            return new SAPContext();
        }
        static JToken WalkNode(JToken node)
        {
            if (node.Type == JTokenType.Object)
            {
                JToken token = node["row"];
                if (token != null)
                {
                    node = ArrayFormatRow(node);
                    node = WalkNode(node);
                }
                else
                {

                    token = node["@nil"];
                    if (token != null)
                    {
                        node = null;
                    }
                    else
                    {
                        JObject temp = new JObject();
                        foreach (JProperty child in node.Children<JProperty>())
                        {
                            temp.Add(child.Name, WalkNode(child.Value));
                        }
                        node = temp;
                    }
                }

            }
            else if (node.Type == JTokenType.Array)
            {
                JArray temp = new JArray();
                foreach (JToken child in node.Children())
                {
                    temp.Add(WalkNode(child));
                }
                node = temp;
            }

            return node;

        }

        public JToken XMLTOJSON(string XML)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(XML);

            JToken node = JObject.Parse(JsonConvert.SerializeXmlNode(doc))["BOM"]["BO"];
            node = WalkNode(node);

            return node;
        }

        
        static JToken ArrayFormatRow(JToken temp)
        {
            if (temp["row"] is JArray)
            {
                return temp["row"];
            }
            else
            {
                List<Object> rowList = new List<Object>();
                rowList.Add(temp["row"]);
                return JToken.FromObject(rowList);
            }
        }

        public async Task<List<List<object>>> comp(JToken json, int level)
        {
            return JSONH.pack(JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json.ToString()), level);
        }
    }
}
