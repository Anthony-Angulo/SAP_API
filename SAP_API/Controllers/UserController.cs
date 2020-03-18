using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase {

        // GET: api/Users
        [HttpGet]
        public IEnumerable<Object> Get() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            
            SAPbobsCOM.CompanyService services = context.oCompany.GetCompanyService();

            SAPbobsCOM.DepartmentsService departmentService = (SAPbobsCOM.DepartmentsService)services.GetBusinessService(SAPbobsCOM.ServiceTypes.DepartmentsService);
            SAPbobsCOM.DepartmentParams departmentParams = (SAPbobsCOM.DepartmentParams)departmentService.GetDataInterface(SAPbobsCOM.DepartmentsServiceDataInterfaces.dsDepartmentParams);
            SAPbobsCOM.Department department;

            SAPbobsCOM.EmployeePositionService positionService = (SAPbobsCOM.EmployeePositionService)services.GetBusinessService(SAPbobsCOM.ServiceTypes.EmployeePositionService);
            SAPbobsCOM.EmployeePositionParams positionParams = (SAPbobsCOM.EmployeePositionParams)positionService.GetDataInterface(SAPbobsCOM.EmployeePositionServiceDataInterfaces.epsEmployeePositionParams);
            SAPbobsCOM.EmployeePosition position;

            SAPbobsCOM.EmployeesInfo items = (SAPbobsCOM.EmployeesInfo)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oEmployeesInfo);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From OHEM");
            items.Browser.Recordset = oRecSet;
            items.Browser.MoveFirst();

            while (items.Browser.EoF == false) {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp = temp["OHEM"]["row"];
                int value;
                if(int.TryParse(temp["dept"].ToString(), out value)) {
                    departmentParams.Code = value;
                    department = departmentService.GetDepartment(departmentParams);
                    temp["department"] = department.Name;
                } else {
                    temp["department"] = null;
                }

                if (int.TryParse(temp["position"].ToString(), out value)) {
                    positionParams.PositionID = value;
                    position = positionService.Get(positionParams);
                    temp["positionName"] = position.Name;
                }
                else {
                    temp["positionName"] = null;
                }

                list.Add(temp); 
                items.Browser.MoveNext();
            }

            return list;

        }

        //// GET: api/Users/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST: api/Users
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT: api/Users/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE: api/ApiWithActions/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
