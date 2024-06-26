﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Xml;

namespace SAP_API.Models
{

    public class SAPContext
    {

        public SAPbobsCOM.Company oCompany;
        //ORYEVBA
        public SAPContext()
        {
            oCompany = new SAPbobsCOM.Company();
            oCompany.Server = "NDB@192.168.0.220:30013";
            oCompany.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_HANADB;
            //oCompany.CompanyDB = "CCFN_PRODUCCCION";   
            //oCompany.CompanyDB = "CCFN_PROD";szzzzzz
            //oCompany.CompanyDB = "CCFN_CORPORATIVO";
            //oCompany.CompanyDB = "CCFN_DEV";
            oCompany.CompanyDB = "SBO_CCFN_PROD";
            oCompany.UserName = "SISTEMAS04";
            oCompany.Password = "Ccfn2023.";
            oCompany.DbUserName = "B1ADMIN";
            oCompany.DbPassword = "B1AdminH2";
            oCompany.LicenseServer = "192.168.0.219:40000";
            oCompany.UseTrusted = false;
            oCompany.language = SAPbobsCOM.BoSuppLangs.ln_Spanish;
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
        static JToken WalkNode(JToken node, string nameNode)
        {
            if (node.Type == JTokenType.Object)
            {
                JToken token = node[nameNode]["row"];
                if (token != null)
                {
                    node = ArrayFormatRow(node, nameNode);
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

        public JToken XMLTOJSON(string XML, string nameNode)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(XML);

            JToken node = JObject.Parse(JsonConvert.SerializeXmlNode(doc))["BOM"]["BO"];
            node = WalkNode(node, nameNode);

            return node;
        }
        public object FixedXMLTOJSON(string XML)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(XML);

            JToken node = JObject.Parse(JsonConvert.SerializeXmlNode(doc))["Recordset"]["Rows"]["Row"];

            List<IDictionary<string, string>> items = new List<IDictionary<string, string>>();
            //node = WalkNodeFixed(node);
            foreach (var Fila in node)
            {
                IDictionary<string, string> Row = new Dictionary<String, string>();
                foreach (var Campo in Fila["Fields"]["Field"])
                {
                    Row.Add(Campo["Alias"].ToString(), Campo["Value"].ToString());
                }
                items.Add(Row);
            }

            return items;
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
        static JToken ArrayFormatRow(JToken temp, string nameNode)
        {
            if (temp[nameNode]["row"] is JArray)
            {
                return temp[nameNode]["row"];
            }
            else
            {
                List<Object> rowList = new List<Object>();
                rowList.Add(temp[nameNode]["row"]);
                return JToken.FromObject(rowList);
            }
        }
    }
}
