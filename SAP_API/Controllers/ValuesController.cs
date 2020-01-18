using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {

        //[HttpGet("us/{id}")]
        //public IEnumerable<string> Us(int id)
        //{
        //    SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

        //    if (!context.oCompany.Connected)
        //    {
        //        int code = context.oCompany.Connect();
        //        if (code != 0)
        //        {
        //            //return [context.oCompany.GetLastErrorDescription()];
        //        }
        //    }

        //    SAPbobsCOM.EmployeesInfo items = (SAPbobsCOM.EmployeesInfo)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oEmployeesInfo);
        //    SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
        //    List<string> list = new List<string>();
        //    oRecSet.DoQuery("Select * From OHEM");
        //    items.Browser.Recordset = oRecSet;
        //    items.Browser.MoveFirst();

        //    while (items.Browser.EoF == false)
        //    {
        //        list.Add(items.FirstName+ ' ' + items.LastName);
        //        items.Browser.MoveNext();
        //    }

        //    return list;
        //}


        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        //// GET api/values/5
        //[HttpGet("{id}")]
        //public ActionResult<string> Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/values
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/values/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/values/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
