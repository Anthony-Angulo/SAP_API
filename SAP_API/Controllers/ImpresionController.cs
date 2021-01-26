using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LPS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using Newtonsoft.Json.Linq;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class ImpresionController : ControllerBase
    {

        // GET: api/Impresion/
        [HttpGet("Impresoras")]
        [Authorize]
        public async Task<IActionResult> GetImpresoras()
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select * From \"@IL_IMPRESORAS\"");
            JToken impresoras = context.XMLTOJSON(oRecSet.GetAsXML())["IL_IMPRESORAS"];
            return Ok(impresoras);
        }

        // GET: api/Impresion/
        // TODO: Authorization CRM
        [HttpGet("Order/{DocEntries}")]
        public async Task<FileContentResult> GetOrder(string DocEntries)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string[] DocEntryList = DocEntries.Split(",");
            List<MemoryStream> pdfList = new List<MemoryStream>();
            PdfDocument outputDocument = new PdfDocument();
            outputDocument.Info.Title = "";

            for (int i = 0; i < DocEntryList.Length; i++)
            {
                pdfList.Add(GetOrderPDFDocument(Int32.Parse(DocEntryList[i])));
            }

            for (int i = 0; i < pdfList.Count; i++)
            {
                PdfDocument inputDocument = PdfReader.Open(pdfList[i], PdfDocumentOpenMode.Import);

                int count = inputDocument.PageCount;
                for (int idx = 0; idx < count; idx++)
                {
                    PdfPage page = inputDocument.Pages[idx];
                    outputDocument.AddPage(page);
                }

                outputDocument.Info.Title += inputDocument.Info.Title + ",";
            }

            MemoryStream ms = new MemoryStream();
            outputDocument.Save(ms, false);
            var bytes = ms.ToArray();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return File(bytes, "application/pdf");
        }

        private MemoryStream GetOrderPDFDocument(int DocEntry)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            OrderDetail orderDetail;
            JToken order;
            string DocCur;
            oRecSet.DoQuery(@"
                SELECT
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    to_char(to_date(SUBSTRING(ord.""DocDueDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDueDate"",
                    to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",
                    to_char(to_date(SUBSTRING(ord.""CancelDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""CancelDate"",

                    (case when ord.""CANCELED"" = 'Y' then 'Cancelado'
                    when ord.""DocStatus"" = 'O' then 'Abierto'
                    when ord.""DocStatus"" = 'C' then 'Cerrado'
                    else ord.""DocStatus"" end)  AS  ""DocStatus"",

                    (case when ord.""DocCur"" = 'USD' then ord.""DocTotalFC""
                    else ord.""DocTotal"" end)  AS  ""Total"",

                    SUBSTRING(ord.""DocTime"" , 0, LENGTH(ord.""DocTime"")-2) || ':' || RIGHT(ord.""DocTime"",2) as ""DocTime"",

                    ord.""Address"",
                    ord.""Address2"",
                    ord.""DocCur"",
                    ord.""Comments"",
                    ord.""DocRate"",
                    payment.""PymntGroup"",
                    contact.""CardName"",
                    contact.""CardCode"",
                    contact.""CardFName"",
                    contact.""ListNum"",
                    employee.""SlpCode"",
                    employee.""SlpName"",
                    warehouse.""WhsCode"",
                    warehouse.""WhsName""
                FROM ORDR ord
                LEFT JOIN NNM1 series ON series.""Series"" = ord.""Series""
                LEFT JOIN OWHS warehouse ON warehouse.""WhsCode"" = series.""SeriesName""
                LEFT JOIN OSLP employee ON employee.""SlpCode"" = ord.""SlpCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OCRD contact ON contact.""CardCode"" = ord.""CardCode""
                WHERE ord.""DocEntry"" = '" + DocEntry + "' ");
            if (oRecSet.RecordCount == 0)
            {
                // Handle no Existing Order
                return new MemoryStream();
            }

            order = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0];
            DocCur = order["DocCur"].ToString();

            oRecSet.DoQuery(@"
                Select
                    orderrow.""ItemCode"",
                    orderrow.""Dscription"",
                    orderrow.""Price"",
                    orderrow.""Currency"",
                    product.""U_IL_PesProm"" AS ""U_IL_PesProm"",

                    (case when ""U_CjsPsVr"" != '0' then ""U_CjsPsVr""
                    else ""Quantity"" end)  AS  ""Quantity"",

                    (case when orderrow.""U_CjsPsVr"" != '0' then 'CAJA'
                    else ""UomCode"" end)  AS  ""UomCode"",

                    orderrow.""InvQty"",
                    orderrow.""UomCode2"",

                    (case when '" + DocCur + @"' = 'USD' then ""TotalFrgn""
                    else ""LineTotal"" end)  AS  ""Total""

                From RDR1 orderrow
                JOIN OITM product ON product.""ItemCode"" = orderrow.""ItemCode""
                Where orderrow.""DocEntry"" = '" + DocEntry + "'");
            oRecSet.MoveFirst();
            order["OrderRows"] = context.XMLTOJSON(oRecSet.GetAsXML())["RDR1"];

            orderDetail = order.ToObject<OrderDetail>();

            MemoryStream ms = new MemoryStream();
            Section section;
            Paragraph paragraph;
            Table table;
            Row row;
            MigraDoc.DocumentObjectModel.Tables.Column column;
            Text text;
            Image image;
            Style style;
            Document document = new Document();
            document.Info.Title = "Pedido CRM";
            document.DefaultPageSetup.LeftMargin = MigraDoc.DocumentObjectModel.Unit.FromCentimeter(.9);
            document.DefaultPageSetup.RightMargin = MigraDoc.DocumentObjectModel.Unit.FromCentimeter(.9);

            style = document.Styles["Normal"];
            style.Font.Name = "Verdana";
            style = document.Styles[StyleNames.Header];
            style.ParagraphFormat.AddTabStop("8cm", TabAlignment.Right);
            style = document.Styles[StyleNames.Footer];
            style.ParagraphFormat.AddTabStop("8cm", TabAlignment.Center);
            style = document.Styles.AddStyle("Table", "Normal");
            style.Font.Name = "Verdana";
            style.Font.Size = 10;

            section = document.AddSection();

            table = section.AddTable();
            table.TopPadding = "-1.5cm";

            column = table.AddColumn();
            column.Format.Alignment = ParagraphAlignment.Left;
            column.Width = "11cm";
            column = table.AddColumn();
            column.Format.Alignment = ParagraphAlignment.Right;
            column.Width = "8cm";

            row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Top;

            image = row.Cells[0].AddImage(@"base64:/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAUDBAQEAwUEBAQFBQUGBwwIBwcHBw8LCwkMEQ8SEhEPERETF
            hwXExQaFRERGCEYGh0dHx8fExciJCIeJBweHx7 / 2wBDAQUFBQ cGBw4ICA4eFBEUHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4
            eHh4eHh4eHh4eHh4eHh7 / wAARCACrAWoDASIAAhEBAxEB / 8QAHQABAAICAwEBAAAAAAAAAAAAAAcIBQYCAwQBCf / EAFgQAAECBQIDAwULBwYHE
            QAAAAEAAgMEBQYRByEIEjETQVEUGCIyYRU3VFdxdIGRkpTSFkJyobGy0xcjUrPR4kdWk8HC4 / AJJCUmJzM0NURVYmNkdYKE8f / EABwBAQACAgM
            BAAAAAAAAAAAAAAABAgMFBAYHCP / EADkRAAIBAwIEAwMKBQUAAAAAAAABAgMEEQUSBhQhMRNBUWGBkRYiIzI1UnGhsdEHM1TB8Bc0cpLh / 9oAD
            AMBAAIRAxEAPwC5aIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiI
            AiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIijLVnW6wtOIcN
            lYqRm52K0OZKSXLEicp7zvhu2SMkZwpjFyeEOxJqKsreLmnRW9pLaaXXHgu9SI1ow4fQCP1lffO2lfitu37A / Cs3LVfQruRZlFWbztpX4rbt + w
            Pwp520r8Vt2 / YH4U5ar6fmhuRZlFWbztpT4rrs + x / dX3ztpT4rrt + x / dTlqvp + aG5FmEVZvO2lPiuuz7A / CnnayvxXXZ9j + 6nLVfT80NyLMoqz
            edtK / Fddn2B + FBxbSp / wXXZ9kfhTlqvp + g3IsyirN52sr8V12fY / uoeLWVH + C67Psj8KctU9P0G5FmUVZvO1lviuuz7I / CnnayvxXXZ9gfhTlq
            vp + aG5FmUVZfO2lPiuuzH6I / Ctq0 + 4nNN7rqcKkzBqNBqLzy9lUYLWsL / AOiHtcd + 7cAnwVZUKke6J3InBFwhvZEhtiQ3NexwBa4HII8QfBc1i
            JCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiA03We7fyJ03q9xsiBkaVhZggtzzP8Oh7gVV7TGjUW2bI / ll1AkW12463FLqZKz
            I5mQ25OHAHI2A2ONgAApl43DjQKp / OIf7HKJNVsjRLTYDYeRDYfolK9WVG1c498m64c0 + lqOqUret9Vvr7jsfxA3hznsZKkQoecNY2XyGhcfOC
            vX4NSvu6iHvX3JXX + ZrfeZ7yuFNHSxy8fgS75wV6 / BqV93CecFevwelfd1EPevuU5mt95k / JXR / 6ePwJd84K9vg9K + 7hPOCvX4PSvu6iJMlRzN
            X7zI + Suj / ANPH4Eu + cFevwalfdk84K9fg9K + 7qJ5SWmZuO2DKwIkaITgNY0uJ + pbRWdPLrolnTN11emukafLgEiMeV7snGze76VeNavL6rZwrr
            RuHLTHj04Rz64NwPEFevwalfdlkLf1n1Kr9RZT6PSJCcmHkAMhSucZ7yegHtKjDSm2pvUS4YdKo7hyjDpmKQcQWZ3J / YPFXb09sih2TR2SFKlW
            CJj + djub6cV3eSVyLeNxVfWTSOrcRXXD2lwUaFvCdR9vRe1 / sYSyqdqNMthTFzTtIlGnBdLS0qHOHsLjt9QW / NlYIZgwoZONzyBdo67//AKvuV
            tIR2rGTyq5uHcT3bUvYlhGAuim1iYlSbfnJSSmGjbtpZsRjv2EfQoE1B1B1asmbMOr0qlmXJIhzUOWBhv8ADfuPsKs0sfXKRT61TI1OqkrCmpa
            K0tfDiNyCMLFWpymvmywzY6RqdC0qJXNGNSHnldfcypXnA3rjPk1KP / 1wsiG2zr7RZyiVijydLvCXgmNT6hKt5O0IGwJG5HQEHPXK1TXLTqPYN
            ycsvzRKPOEvlIh6tx1YT4jI37wufDYS3V + kYJGecHH6JWvo3NelWUWz1LVNC0W + 0ad5aU1HEXJNdO3k / wC5L / BjetSrloVG1q9MGLVaBMmXPNk
            vcwHGTnuBx8uSVP6qxwi4GuGpoAwPKH4wP / MarTre10lUeDxOPYIiLCSEREAREQBERAEREAREQBERAEREAREQBERAEREAREQBERAQhxu + 8FUvn
            EP9jlE2q / vJ6bfMv9EqWeN33gal84h / scom1X95PTbHwL / RKx3v + zf4naOCvtyj7 / 0Ii78IubGPiRBDhtLnOOAACSfkAXN0vMMqDac6BEbOlzW
            iXc0iIS4AtHLjO + f1rrnV9j6HnWpw6SkkdRxhfPzv8ysLpfw8xZ6XhVG8o0SXhvAc2SgnDseD3dQfYMfKsrxP2Va9p6EVGLQaPKycdkaABGZDH
            aH0gDlx3P0lcyFlUksvodKv + PtPt66oUk5vOMrt8fMrlRaZP1mqQKZTJWJNTcd4bChQxkn + wDvPcrF2Lw3SrYUKau + pvixiATKSnotb7HPO59u
            MfKvfwfadm3bIh3TVw6JVqqwPhdpuYMD80DwLuv1Lc9SdYrVsuadT40SJPVFvrS8vvyeHM47D5Oq5FK1pU476p1TVuL9S1Wvyulxa9cd37 / JG0
            WvZtsWzAEOi0aUlSPz2sBef / kd / 1rQuLppdoXWobGlznGGAAOpLsYUd13iZqsXmbRrfloA7nTEQvP1DGFr9Cvy7NTrzpNt12chPpcxNsfGlocJ
            rWuDTzbnqRkDvWV3VLGyPmaiPCGsYd3ddFHq9zy + nUmrhi08l7D02lO1hM91akwTM5EwM + kMtZ8gB + slSbVqhJ0unx6hUJiHLysBhfFivdhrQO
            8r0MYGMDGgAAAAAbDCrhxi3PMMNMtOXiFkGK0zUyAfWAOGA + zIJ + UDwWerUVGnuNDpGnVNa1CNDONz6v0Xmee9 + JObdOvl7RpkISzSQJmcBJfj
            vDARgfKc + wLH2lxI1 + BPw2XLTpOaky7D4kq0sewHvAJIOPDZQPvjcZKDphad3dVyzk9uhwVo8aHg + Fn29c / E / ROhVWSrVJlqpTozY0rMsESG8d
            4Xv2ChXhDqEea08mJSM9zmys0Wsyc4BGcD5FNZW6oz8SCkeD6tY8he1bbOdrx7iNeI + iQK1pTVHPY0xpJomYLu9pad8fK3I + lVr4biP5X6N4 + n
            + 6VbXViXdNaZ3NAbs51LmOU + 0Q3EfrCqJwxxhG1WocUY9IPzju9FcK6h9PCR3rhS83aHe2zfZNr3o3 / hF9 / HU35w7 + sarTKrHCL7 + Opvzh / 8AW
            NVp1ubj + Z8DzOPYIiLAWCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgIR43PeCqXziH + xyjK + qXO1rSrS6l06C6NNTMqGMaBnOQdz
            7Apv4krNq1 / aYzFs0V0Fk5Mx2lr4xcGNAa45cQCQO7p3heyx7Wlbdsq3o1YMCPOUOncgiwnc0P1cuc04GcjbKV4qpb7G / M2ei38tOvY3MVlrOP
            xawjE6W6U25Y1KhzlRbLzVVc3MaZj45WbdG52A / WVqumlkU2vcQN66hzTYU1LSc3ClKYR6TOcQGc7x44BDR7ebwUMar6mVy9q3HJm4svSWvIlp
            WG8taWg + s7HrE9fZ + 2edGoj6Bw1PqkABscSs1MtP / AIg55GfaMBa + jWhKWyC6I7drWi39vbRvLurmrVajj0z1 / wASMtqbrZbdnzUSmy7HVOos2
            fChEcrD4Od0B9ihK7dU6vqrEk7OnadKylOnp2FziG4uecOBxlRJMRokeO + YjPc + JEcXPc45JJOckrP6Ye + FQfn0P9q4crypUljOEd0ocFabp9l
            KpKO + oot5frjyRd255k23Yc5Hp0u5xkJE9hCY3JJaz0RgdegVNZHTvUK5pyLOihT0SLHiF740ccgc4kknJV6xgtAIBGO9cfRb0wFs69sqzXXoj
            y / QuJquiwqeDBOUvN + SKD6pWRXNN6dTpy5GQIfl8RzITITw8tLQCckfpBe / hnqsjNaxUWHDiAvc52GkbnZSH / ug7mmg2pjB / wB8zHT9FihHhPy
            NeLfz0L3 / ALq4vKwhUWDtL4tv7zTqirY + cmux + jAHVU / 4vDnVSCPCmw8fbergdyp9xeEfypwv / boX771mv / 5Rqf4e / bC / 4shxE7kytKe9lrODU
            / 8AEyqD / wBYP2KeCoF4NCPyPq3j5WP3Sp7wt9aP6GJ838W / bNx + P9kY25JYTtv1GUPSNKxYf2mkKjnCXF / 5W6dKuJLoMWKME926vm9oc0g7gjd
            UH4eWOpfFK2nAEtbOzMI92MF2 / wCpK8dziV0O6dvTrrylFr4olPhF9 / HU35w / +sarTqDdBdNa / Zmpl3XBUo0nGkq650aVdLuc7DS9pAdloGSOm
            Cc4KnJc + vJSnlHXUERFhJCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCwvkbZ + gz0kTyiPEmYZPhmI8Z / Ws0tfdIVqFUocWWjS / k
            zHxi6E6MR2nO8uGRyHBGe5Qy8HiWc4wVQmNA9QWVWJKQZOViQGuIZMGPhjm52JGMg + zCmvh9hSle0ZnLWm385kZqcpM4GnGHNe7OD4YcCCpTLq
            sQR5JJfenfw1HelemlWsKv3DUZWqMnZatzT5uJKRY2Gw4rnE8zSIeehx7cDwXGp2sacsxOz6nxVearbxpV2ltaawvNEeTPDDGM24y91NbL83oi
            JK5cB4ZDh9eFhbs0tl9M7is2oQarGqEWcrMOA5r2BrW9 + QBvn6VaLmq2f + iSX3p38NahqLZM7ecehxZh8vKmkT7Z1gZHJ7QgY5TmHsnKUl1S6j
            5Y6tVXh1a3zez6L9jHcS13VuyNKJ2v29MMgT8GNBY174bXgBz2tOxGDsVTWd191MnifKrimtzuIJEH9wBXZ1fsed1FsqYtibfAkYUeJDeY0KOX
            OHI4O6GHjuUH + Z5K / 4zRvtD + GrVacpM4WlX1C1hmaW7Pon + qK7VS + p2tFprkacqBaSWmYjmJyk9cc2cLf + GGepExrTQ2QZYQ45c7lPIBg48cqS
            hweyv8AjNG + 0P4a2XTDhsbY16yNzytaM3FlCS2DEicrXZGNyIeVgja4luOwXXFaq20qHRprH1V + xYvuVPuLoY1Vhd3 / AAbC / ferZ89X + CSX3p3
            8NRTqno3Fv25m1yZn2ScRsu2B2cONzAhpcQd4fX0lmuqcqlPbE1XCGqW + l6iri4eI4a6dSn + fblfQe8nCsp5srf8Avt3 + U / 1aebKzvrbv8p / q1
            rOSreh6t / qBov33 / wBWZPgy3tOr7 / 8Aa2 / ulT6o30k0 + nNPKdNyUpGgzrZmKIhMWOW8uBjuhreeer / BJL707 + GttbwcKaizxriG8pXupVbii8x
            k + nwPceio / YUo2S43puWaMNbVpsj5DzFXS56v8EkvvTv4aiWW0UdA1sfqcyeZ5S6MYpk + 29DJby + t2eVecW8YOFaV40lNS81gl2hf9SSHzaH + 6
            F7F56dBdLU + Xl3EOdChNYSOhwAF6Fc4YREQBERAEREAREQBERAEREAREQBERAEREAREQBEUWa9avymmMrT5eFSotWrFTithyUq14Y1 + XY3duRv
            t07 / YpjFyeECU0Ve7j15u2zLddNXtp8yQqcy + FDpktAng9syX59YkZZjB7iuyztea7Magy9mXjacGiVCclDNShgRzGbEBa5zQc4I6eBzvjqFk8
            GeM4I3IsAirTYXFjbtXpNwTFyU5tGmqZCMaVgsi9p5Y0Hl5WkgHnzjbHfnuXTTuJ6ozuklVvsWpKw306pskXSZmyS4OYHB2cZHU9xCnl6mcYI3
            Is4irxaGvN11mRjVSbtOgy1PhyL5vmg1xkaNsMtaYTfSbk7ZIGFrVO4ra0aJIXBU7FlIVJnZryRkSBUeeM1 / j2ZaCR9Iz4jKcvUfZDci1iKAar
            rzX6pqBM2Xp5ZDK7PyMARpx8ec7JjNgS0bZJ9Jo3xvkL10bW6tTmslI04nrWl6dNT1OZMxohm + 1MvFMIvdD9EYdggjIIUeDMncic0VaXcT8R2o
            nuFDteGaH7pmnCpmZOTEGD6uMDOQOpxnO + MHaqbrbNT2ur9N4VFlzLhvaMnxFdlzC8AehjwPj17u5HRmvL2jKJsRYm76q + iWxUqxDhCM + Tlnxh
            DJwHcozhVto3FTXX2zAvCr6dNh2w + e8hiTktUA58OIA1xHIWgn0XezfbO4zEKUprMQ3gtQirjfnEdUqRqJDta37ZptRhR5SDNwJmbqQlWuZEht
            fu5w5R62xzupZ0muqt3ZQTUK7S5GnRjgshyk42ZYW872hwiN2IPJnZRKnKKTYTybqigSra6XFVtQKpaOm9lQ7hi0lhfOzEab7FgAOPRBGT1aMY
            6nvC8 + qHEHVrOh0KQgWcydr07IRp2fkTNbSjITS5 + HNB5hhrznYDlO5VlRm2khlFg0UA3 / xDGhaU2jfVJoUGoNuA8jpeJMcnYxABzNDsHOHZbn
            bpnZeOhcRNWmLmqVr1i0ZSUq0pT3TzHS1Q8oguY1oeQ4hoIJGB7D1TwJtZwNyLFIqp0 / i6hTViVGsm2YEKryUeG0SDpp3LFhPcB2jXcvd4YWcv
            biKr9Buig29KWnTpmaq9Lgz4fMVES0NhiBx5eZwLQAANyd842VuXqZxgjciyCKt13cRtete3qHGqln05tWrkzFhyjYVVbElGwmOa3ndHaC0kuc
            4ED1eXJPcpP0fvS5LtlYsW4KBIUssLuxiSVQZNwpgDk9Jr2EgesQQd1SVKUVuZKaJCRQDrFr1WbJ1SZZFNtmnVB8aCyJDjzVQEs0czQcFzhyjr
            1JCwt08Rt0W / TqDEmrNpEWdrM1FgQ4MCqCJDYGloae1aC12S49OmO9SqE3jHmNyLMIq2y / EfPzdJvimzdtQqZcNt01823sZxsxAfhzG + i7lxzD
            tA7GCNjkbELG2xq1ULO0Mo19zkzVbkq9wzZl5eSn5tpGQ8tIaWMaBg8v5p6nJ32nwJjci0iKvNK4kWRtJaxeVQt4QJ + lTjZWPT4cfmLXHOMkjO
            Nhv3c3fjfMaIaw3Rf8AWpSFPWtSZamTUB8Vs1JVVkd8EjcNiQ / WaTjG + NyFV0pxTb8hlE3IiLGSEREAREQBERAEREAREQBERAEREAUKcSGlNW1
            BnaJVqBHErVqLFZGlYkQNdDcQ8uIcC4HYhpHXvU1orRk4vKIayVmv7SbVTUWjMfdtRp7avT48ONTXysKGIbS0kkP9IE5yei7re0iv6oaoS1 + 33
            NS8zOU + Q8lk4MkGBpcGFrXPJeM4znx26qyaK6rzSwhtRWPSjholabbUGFesnAnqnTao + fk3Qi0NjNcxo7J25yOZgPpZHTIIysDJcOV6wdKbitR
            81KGZqdWZOw3hzeVrA0ggjm67D6x13xmNSLz1IneJh + nNq3FBpkvFkTMwzGa4ta5kN7znDh15Md3j4rSpjXq + pjRqrVYTQla7RatDkosWG97oU
            Zr2u9LBOQfQIxkjvABJXJXjPrnvj / wr0RullaKXzRpKYp8xSrbhQo9OdKPmJOAxkd7uUcpc4u6Z3J6n6Vrclwv3NTrbt99PdLMuGm1F0xHmHOa
            YUWHsWt5S / qCD3YXpsbUu6qnbFXq8xqBHnZyUpZmWSTJCPCa2IcetEdhpAGdmk526KYOE6763eukUGvXHOCYnok7Fhl / TYBuBuT4qkp1YZYSTN
            HiaTalW1qtV77sCbk5Z9Zg4m5ecawhjzyl3Lh5BBc3IB6Z + rhUdINRp3VaFqDHmpOJURRTJxSQ0B0yYLmF4HPgNy7IHcANl6uMLUK8bGiUD8k5
            10KPPTb4DmHmIeAxha0AEb5cd + pz7FpVm6 + 1u49T4DIUeM2hQ7fiz03IdrEERkzBgudEZzl2cc7Tj2EK0Y1ZR3rAeF0OgcLV5fkZ5KbimBVvLv
            KxBHZ + S8 + fXzzc3NgD2bLPxdGNV5LUqDfVCnKXAn2ycOA4RmteC9o3OC7xGVqbtVdWzpg7Vr3almUj3T8mbSyInpM5nDZ / NnI5Dk57xsvbqNrR
            dELUKmSVNuf8AJ + iztFgzxdMCJFLHuYSW5LsnJGFf6aTx0I6ItFcdOrNU03j0qOyHGq03ThAmCwhre1cwB7h0GOYk42VYqTw6anHT + Fp3UaxIQ
            bbNT90Y4gQmOjOfyhuxLtth44S2L / 1guzSusXfJV2Wp8GhiO50dzYp90GNY54LWlx5SAwg7j1h4LFs1F1hZoO / VSLd8uZd8z5JClRDfztf2gaX
            E82MYB7 +/ oqU6VSHRNd / zJbRtV8cP93VPUOWuOlSVGfJy0jBlIcnUWsjMxDhtZkgux + bnfOPb1U4aJW7XratyJIV6Vp0rEDh2cKQY1kFoBd6rW
            kgbcvh0URcON7XVeVz08T1 + NqA8lEzMyHkMeFy5wSA9w5HY3GxOeoK2Diyvq / LPg0ttpwosKRjkOnJyHAdEMABxBOG7 + H + xWOe + clTZKx3MHL6
            Q6l2fqVV7u0 + n5OCKszEzBnWsdyvOOYtw48w5hnfHybDHgmtBNR7q1Em7pui6hIxYtO8jbFk3Bri1zOWIz87DXc0QkAY9IjI6LfOGK9otz0yrx
            KhfEpcToboZhYhPgxIAweYPa / oclvTZfOKDUCr25T7coVnzkKHcFfqsKTlYhiYa0OcASSCARlzQSTgcwPtRTqb9q7jo0RRE4cL / AI + mElZcxNS
            USHIVd87AfloHZPaA5nr9cgnp3nfpjYaDoHc9uXncE3RewhUaqyDpfsGxBzMeW4DgS7du7hg42K3PhS1Dq940mu0i5ppsxXaLUIkCYc1xIc0OI
            a4Ak + BG39HPepuY4PaHNIcD0ISpWqRbiwooplP8J1xRrApcrCnJaHX5aZiCO8EdnEgEgtB9LqDnvPRbBfWgl7Vq7rfrkpK0eJDpNJgyLpaeayL
            DiuYHZcRzYIJdkA + Hf1XC6r61PqvELXrEtu5YFOl5WDGmIPbse5oENrnFpwe8DHT + 1a / WOIK / JrQqJW4UeDKVunVuFIRo0MHliMLH5Ox65Z3k +
            OTtjNmvLDz / AIyMRN + qml + pU9ZtMoMSj2fFl5N8cGSfIwhLtY8hzSzBy13MYhP6QIwtk4W9Ka9plJ1OHW5xkZ05EL2woRHZQvV2b6R8Dnp0CxX
            DPeFwXTckw6o3v7twIVPD4kp5BHgmFEe5paeaIMOxyvG2evXffv4o9TbltSv23aFqvhS1QrcQATUXm5YQL + XOxyfHCwvxG / DJ6dz5qRonP3fxB
            U69J6BIzlvwYcKHMSscB3aBoAOxO / Q9y8WumgsS6qpbMvaVKp1Mo1IiPfGgQ + VjYoeWlwABGNm4 + lSDp9Sr / t8VBt73JL1mGZZzoEWXLmOa7b0
            cOPXrggd4UIVvVi9bV1Quqg1mrxH06Xo8Wep3MXNc4sYXAEk5I5muG2CenXZKbqSaUX2DwZOi8P10UWDqTSKdFlBSLjlXQ6exxBfBidq0syS7p
            yF47v8AMsdF4eL8nKXZFFmKqyUkbdY90SJLcnamKYjnh7MvwcZaBnGMH5TK / CrXrquLS6Dcl6VBsaaqMZ75VrgW8sFm2RlxzuHHI / X3TCodapC
            T6jCwVUt / QzUe1qrcrqNUpKoydZwXe6sCFFc9wOSXt5iAT0yPBe7SDQi6rd1Vh3lVW06nQIQaBJUzlZCecgFzm5PdvgY38eis6iq683n2k7UER
            FhJCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgIZv7QOnXVqM++oV3V6iVV0AQA6nljC1uHA4cQTuHYK80zw1WMdNo1kSM7VZaBMTjZyYnHRGRI8
            aI0EZcS3GMHoAOnykzeiyKtNYWexGEQrQtBRTafGpcfUS66jTIkm + UElMxIboTA4ABwHL1bjZNL9ApSwalJR6dfNzTUjKRe1bTY0Vvkzn / wBIs
            aAM7dVNSI6s2sZGERrq9pHSdSZ6jTdUqs7JmkznlUESzW + k7DBg8wO3oDosRTuH2zadqtPX7JxpuG6ebGbHpo5fJiYzS2JgY5gHZJxnYnbZTCi
            KrNLCYwivruFm1jDdS23bczbbfNCadRxGh9kXjP53LnG / T2nfvW1T + hdrTmocvd7pmZZ2FNFOZIBjDAEMMLO8E53ypYRT41T1G1EPWZoRSLXsW
            u2bJ3FVI9LrDIjXtjNYXQOdjmZYQABgOJ3BXB + gFvu0TbpYa3U / c5s55X5Vys7bm5i7GMcuM + xTIijxZ5zn2jCIm060cmbLqcjMQNRbpqMnJwx
            DhU + bisMuGjAA5QB0AwP9gs7qbp9M3lNSs1JXlcFtxpdhY73NihrYwycc7SMHHM76x9O + Iqucm9z7jBBdI4dpGh2jXKRQLzrMjUaw9sWPVuRhm
            Gva7PolvKQ0gkFoO + euwXfWuHa37kqVtTd23DWK4yiSnk7oEZ4a2bdklz3uHpguJbnDujRvndTait4085yMIhGkcO9Dtuo3HN2dcdYoDa1Ldg2
            DAcHNlcEEOY4kPJHpDdxOHEZ71IWldqTdlWXKW9OXBOV + LLucTOzQIiPyc4OXO2G / f / atsRRKcpdwlgg + 7eHemV7UGfvSFeVw0ioT3M2L5A5kP
            DHZBaHEE4IOCuVT4b7OmtMJewZapVWUkYc8J + JHD2vixY3LyknmHKBjwHhuVNyKfGn069hhEa6X6XzdkVjy19 / XNXpZsoZZknUIrXQYYy0hzQA
            MEBuPkJXZrLpJQNTodPiVKcnadPU6Jzys7JlrYsM9diR44PsxtjKkZFXfLdu8xghaoaHVKZtaYpcPVS7BUY8dkQVKK8PiQ2tH / NtDS0tad84O5
            O + V0aj8OlAvkUuJU7hqkvNU + U8k7eWY0GOwgD + c5s5JPMdiB6R2U4IrKrNPKYwiKqrpA91Xs2LQb1rVEpVsshwjTYG8OdYwg / zmCBl2CHZaQQd
            g3vlKEzs4TGZzytAyuaKjk33JwERFACIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIiAIiIAiIgCIi
            AIiIAiIgCIiAIiIAiIgCIiA / 9k = ");

            image.Height = "3.5cm";
            image.LockAspectRatio = true;
            image.RelativeVertical = RelativeVertical.Line;
            image.RelativeHorizontal = RelativeHorizontal.Margin;
            image.Top = ShapePosition.Top;
            image.Left = ShapePosition.Left;
            image.WrapFormat.Style = WrapStyle.Through;

            BarcodeLib.Core.Barcode barcode = new BarcodeLib.Core.Barcode();
            barcode.IncludeLabel = true;
            System.Drawing.Font font = new System.Drawing.Font("Verdana", 13f);
            barcode.LabelFont = font;
            System.Drawing.Image I = barcode.Encode(BarcodeLib.Core.TYPE.CODE39, orderDetail.DocNum.ToString());
            I.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            byte[] imageBytes = ms.ToArray();
            string base64String = "base64:" + Convert.ToBase64String(imageBytes);

            image = row.Cells[1].AddImage(base64String);
            image.Height = "2.75cm";
            image.Width = "7.5cm";
            image.RelativeVertical = RelativeVertical.Line;
            image.RelativeHorizontal = RelativeHorizontal.Column;
            image.Top = ShapePosition.Top;
            image.Left = ShapePosition.Right;
            image.WrapFormat.Style = WrapStyle.Through;

            table = section.AddTable();

            column = table.AddColumn();
            column.Format.Alignment = ParagraphAlignment.Left;
            column.Width = "9.5cm";
            column = table.AddColumn();
            column.Format.Alignment = ParagraphAlignment.Right;
            column.Width = "9.5cm";

            row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Top;
            paragraph = row.Cells[1].AddParagraph();
            paragraph.AddText("# pedido: " + orderDetail.DocNum);
            paragraph.AddLineBreak();
            paragraph.AddText("Fecha de Creación del Pedido: " + orderDetail.DocDate + " " + orderDetail.DocTime);
            paragraph.AddLineBreak();
            paragraph.AddText("Fecha de Entrega Pedido: " + orderDetail.DocDueDate);
            paragraph.AddLineBreak();
            paragraph.AddText("Estatus de Pedido: " + orderDetail.DocStatus);

            row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Top;
            paragraph = row.Cells[0].AddParagraph("Compañia");
            paragraph.Format.Font.Bold = true;
            paragraph = row.Cells[1].AddParagraph("Enviar A");
            paragraph.Format.Font.Bold = true;

            row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Top;
            paragraph = row.Cells[0].AddParagraph();
            paragraph.AddText("Av.Brasil 2800, Col.Alamitos");
            paragraph.AddLineBreak();
            paragraph.AddText("Mexicali Baja California 21210 Mexico");
            paragraph.AddLineBreak();
            paragraph.AddText("Telefono: 6865541535");
            paragraph.AddLineBreak();
            paragraph.AddText("Forma de Pago: " + orderDetail.DocCur);
            paragraph.AddLineBreak();
            if (orderDetail.DocCur == "USD")
            {
                paragraph.AddText("Tipo de Cambio: " + orderDetail.DocRate);
                paragraph.AddLineBreak();
            }
            paragraph.AddText("Tipo de Pago: " + orderDetail.PymntGroup);
            paragraph.AddLineBreak();
            paragraph.AddText("Observaciones:");
            paragraph.AddLineBreak();

            if (orderDetail.Comments != null)
            {
                paragraph.AddText(orderDetail.Comments);
            }

            paragraph = row.Cells[1].AddParagraph();
            paragraph.AddText("Enviar A: " + orderDetail.CardName);
            paragraph.AddLineBreak();
            paragraph.AddText("Nombre: " + orderDetail.CardFName);
            paragraph.AddLineBreak();
            paragraph.AddText("Codigó: " + orderDetail.CardCode);
            paragraph.AddLineBreak();
            paragraph.AddText("Dirección: " + orderDetail.Address2);
            table.AddRow();

            table = section.AddTable();
            table.Style = "Table";
            table.Borders.Color = Colors.Black;
            table.Borders.Width = 0.25;
            table.Borders.Left.Width = 0.5;
            table.Borders.Right.Width = 0.5;
            table.Rows.LeftIndent = 0;

            column = table.AddColumn("3cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            column = table.AddColumn("3.32cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            column = table.AddColumn("3.16cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            column = table.AddColumn("3.16cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            column = table.AddColumn("3.16cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            column = table.AddColumn("3.16cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            //        column = table.AddColumn("2.62cm");
            //      column.Format.Alignment = ParagraphAlignment.Left;

            row = table.AddRow();
            row.HeadingFormat = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.Format.Font.Bold = true;
            row.Cells[0].AddParagraph("Codigo");
            row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[1].AddParagraph("Producto");
            row.Cells[1].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[2].AddParagraph("Costo Unitario");
            row.Cells[2].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[3].AddParagraph("Qty Pu");
            row.Cells[3].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[4].AddParagraph("Qty SU");
            row.Cells[4].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[5].AddParagraph("*Cantidad");
            row.Cells[5].Format.Alignment = ParagraphAlignment.Left;
            //          row.Cells[6].AddParagraph("Total");
            //            row.Cells[6].Format.Alignment = ParagraphAlignment.Left;

            //table.SetEdge(0, 0, 6, 1, Edge.Box, BorderStyle.Single, 0.75, Color.Empty);

            double boxes = 0;
            for (int i = 0; i < orderDetail.OrderRows.Count; i++)
            {

                row = table.AddRow();
                row.Borders.Left.Visible = false;
                row.Borders.Right.Visible = false;
                row.Cells[0].AddParagraph(orderDetail.OrderRows[i].ItemCode);
                row.Cells[1].AddParagraph(orderDetail.OrderRows[i].Dscription);
                row.Cells[2].AddParagraph(orderDetail.OrderRows[i].Price + " " + orderDetail.OrderRows[i].Currency);
                row.Cells[3].AddParagraph(orderDetail.OrderRows[i].Quantity + " " + orderDetail.OrderRows[i].UomCode);
                row.Cells[4].AddParagraph(orderDetail.OrderRows[i].InvQty + " " + orderDetail.OrderRows[i].UomCode2);

                //row.Cells[6].AddParagraph(orderDetail.OrderRows[i].Total + " " + orderDetail.DocCur);
                if (!orderDetail.OrderRows[i].UomCode.Contains("CAJA"))
                {
                    double pesopromedio = double.Parse(orderDetail.OrderRows[i].U_IL_PesProm);
                    if (pesopromedio != 0 && orderDetail.OrderRows[i].UomCode.Equals("KG"))
                    {
                        boxes = Math.Ceiling(orderDetail.OrderRows[i].Quantity / pesopromedio);
                        string cajas = boxes > 1 ? " CAJAS" : " CAJA";
                        row.Cells[5].AddParagraph(boxes + cajas);
                    }
                    else
                    {
                        row.Cells[5].AddParagraph(orderDetail.OrderRows[i].Quantity + " " + orderDetail.OrderRows[i].UomCode);
                    }
                }
                else
                {
                    string cajas = orderDetail.OrderRows[i].Quantity > 1 ? " CAJAS" : " CAJA";
                    row.Cells[5].AddParagraph(orderDetail.OrderRows[i].Quantity + cajas);
                }

            }

            //row = table.AddRow();
            //row.Cells[0].Borders.Visible = false;
            //row.Cells[1].Borders.Visible = false;
            //row.Cells[2].Borders.Visible = false;
            //row.Cells[3].AddParagraph(boxes + " CAJAS");
            //row.Cells[3].Format.Alignment = ParagraphAlignment.Right;
            //row.Cells[4].Borders.Visible = false;
            //row.Cells[5].AddParagraph(orderDetail.Total + " " + orderDetail.DocCur);

            //table.SetEdge(5, table.Rows.Count - 4, 1, 4, Edge.Box, BorderStyle.Single, 1);

            paragraph = document.LastSection.AddParagraph();
            paragraph.Format.SpaceBefore = "1cm";
            paragraph.AddText(@"Elaborado Por: " + orderDetail.SlpName);
            paragraph.AddLineBreak();
            paragraph.AddText(@"Hora de Impresión: " + DateTime.Now.ToString("HH:mm"));
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(@"*La cantidad de cajas es un aproximado en base al peso promedio por caja del producto", TextFormat.Bold);

            paragraph = new Paragraph();
            paragraph.AddText("Page ");
            paragraph.AddPageField();
            paragraph.AddText(" of ");
            paragraph.AddNumPagesField();

            section.Footers.Primary.Add(paragraph);

            document.UseCmykColor = true;
            const bool unicode = false;
            const PdfFontEmbedding embedding = PdfFontEmbedding.Always;
            PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(unicode, embedding);
            pdfRenderer.Document = document;
            pdfRenderer.RenderDocument();
            ms = new MemoryStream();
            pdfRenderer.PdfDocument.Info.Title = orderDetail.DocNum.ToString();
            pdfRenderer.PdfDocument.Save(ms, false);
            //var bytes = ms.ToArray();
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            //return File(bytes, "application/pdf");
            //order = null;
            //oRecSet = null;
            //DocCur = null;
            return ms;
            //return File(new byte[] { }, "application/pdf");
        }

        // POST: api/WmsTarima
        // TODO: Change this to /Tarima for WMS WEB AUTHORIZATIOn
        [HttpPost("WmsTarima")]
        [Authorize]
        public void PostWmsTarima([FromBody] TarimaPrint value)
        {
            etiquetaproduccion(value.IDPrinter, value.WHS, value.Pallet, value.Request, value.Transfer, value.RequestCopy, DateTime.Now.ToString());
        }

        // POST: api/TarimaImp
        [HttpPost("Tarima")]
        public void Post([FromBody] TarimaPrint value)
        {
            etiquetaproduccion(value.IDPrinter, value.WHS, value.Pallet, value.Request, value.Transfer, value.RequestCopy, DateTime.Now.ToString());
        }

        private void etiquetaproduccion(string IDPrinter, string WHS, string NumeroTarima, string SolicitudTraslado, string Transferencia, string Recepcion, string Fecha)
        {

            //string s = "^XA\n";
            //s += "^FW\n";
            //s += "^CFA,40\n";
            //s += "^FO50,30^FDSucursal: " + WHS + "^FS\n";
            //s += "^CFA,40\n";
            //s += "^FO550,150^FDTarima ^FS\n";
            //s += "^FO570,220^FD" + NumeroTarima + " ^FS\n";
            //s += "^CFA,35\n";
            //s += "^FO40,100^FDSolicitud de Traslado^FS\n";
            //s += "^BY3,2,70\n";
            //s += "^FO40,150^BC^FD" + SolicitudTraslado + "^FS\n";
            ////'---------------- Tranferencias generadas para la Tarima 1
            //s += "^ CFA,40\n";
            //s += "^ FO40,350^FDTransferencia^FS\n";
            //s += "^ FO40,380^FD" + Transferencia + "^FS\n";

            ////'-------------------
            //s += "^ CFA,35\n";
            //s += "^FO350,450^FDRecepcion^FS\n";
            //s += "^BY3,2,70";
            //s += "^FO350,500^BC^FD" + Recepcion + "^FS\n";
            //s += "^CFA,20\n";
            //s += "^FO40,570^FDFecha: " + Fecha + "^FS\n";
            //s += "^XZ\n";

            string s = "^XA\n";
            s += "^FW\n";
            s += "^CFA,40\n";
            s += "^FO40,50^FDSucursal: " + WHS + "^FS\n";
            s += "^CFA,40\n";
            s += "^FO550,150^FDTarima ^FS\n";
            s += "^FO590,200^FD" + NumeroTarima + " ^FS\n";
            s += "^CFA,35\n";
            s += "^FO40,100^FDSolicitud de Traslado^FS\n";
            s += "^BY3,2,70\n";
            s += "^FO40,150^BC^FD" + SolicitudTraslado + "^FS\n";
            //'---------------- Tranferencias generadas para la Tarima 1
            s += "^CFA,40\n";
            s += "^FO40,350^FDTransferencia^FS\n";
            s += "^FO40,380^FD" + Transferencia + "^FS\n";

            //'-------------------
            s += "^ CFA,35\n";
            s += "^FO300,420^FDRecepcion^FS\n";
            s += "^BY3,2,70";
            s += "^FO300,470^BC^FD" + Recepcion + "^FS\n";
            s += "^CFA,20\n";
            s += "^FO40,550^FDFecha: " + Fecha + "^FS\n";
            s += "^XZ\n";


            var bytes = Encoding.ASCII.GetBytes(s);
            // Send a printer-specific to the printer.
            RawPrinterHelper.SendBytesToPrinter("\\\\192.168.0.10\\" + IDPrinter, bytes, bytes.Length);
        }



        // POST: api/Impresion/Carnes
        [HttpPost("Carnes")]
        [Authorize]
        public void Carnes([FromBody] CarnesPrint value)
        {

            string txtProducto = value.ItemCode;
            string txtDescripcion = value.ItemName;
            string txtLote = value.Batch;
            string txtCaducidad = value.ExpirationDate;
            string txtPeso = value.Count;
            string txtSerie = "56";
            string txtSecuencia = txtSerie.PadLeft(14, '0');
            string PesoRelleno = txtPeso.Replace(".", "").PadLeft(6, '0');
            string gtin = txtProducto.PadLeft(14, '0');
            decimal peso = Convert.ToDecimal(txtPeso);

            string s = "^XA\n";
            s += "^FW\n";
            s += "^FO20,60^GFA,3000,3000,30,,T07gPF,T0gQFC,S03F8gN0FE,S07CgO01F,S0FgQ078,S0EgQ03C,R01C1gF8I01JFC1C,R03C3YFCK07IFE1E,R0387YFL01JF0E,R038YFEM07IF8E,R038YF8M03IF8E,R030YFN01IF87,R030XFCO07FF87,R030XF8O07FF87,R030XFP03FF87,R030UFCFEP01FF87,R030TFE0FCQ0FF87,R030TF00FCQ07F87,R030SFC00F8Q07F87,R030RFEI0FR03F87,R030RFJ0FR03F87,R030QF8J0EJ01FL03F87,R030PFEK0EJ0FFEK01F87,R030PFCK0CI03IFK01F87,R030PF8K0CI07IF8J01F87,R030PFL08I0JFCJ01F87,R030MFCFEL08001JFCJ01F87,R030LFE0FCK018003JFEJ01F87,R030LF00FCK0FI03JFEJ01F87,R030KFE00F8I01FFI07JFEJ01F87,R030KF800FI03IFI07JFEJ01F87,R030KFI0F001JFI07JFEJ01F87,R030JFCI0E007IFEI07JFEJ01F87,R030FFC7CI0E00JFEI0KFEJ01F87,R030FF078I0E01JFEI0KFEK0F87,R030FC07J0C01JFEI0KFEK0F87,R030F806J0C03JFEI0KFEK0F87,R030F006I0F803JFEI0KFEK0F87,R030E00400FF807FE3FEI0KFEK0F87,R030C00403FF807C03FEI0KFEK0F87,R030800407FF8J03FEI0KFEK0F87,R03080380IF8J03FEI0KFEK0F87,R03I0F81IF8J03FEI0KFEK0F87,R03003F81IF8J03FEI0KFEK0F87,R03007F81IF8J03FEI0KFEK0F87,R0300FF83IF8J03FEI0KFEK0787,U0FF03IF8J03FEI0KFEK07C,T01FF03IF8J03FEI0KFEK07C,::T01FF83IF8I07FFEI0KFEK07C,T01FF81IF80KFEI0KFEK07C,U0FF807C180KFEI0KFEK07C,U0FF8I0180KFEI0KFEK07C,W08I0180KFEI0KFEK07C,W0CI0180KFEI0KFEK07C,:W0CI0180KFEI0KFEK03C,W0EI0180KFEI0KFEK03C,:,:::::::::::::hQ01C,,001F1F8E0E3F9FC3F38787003F8FF00FC7C3F8E39FE7E03FDFE38787E,003F3BCF1E3F9DE7B38F87003FCFF01EC7C3BCF39FCF603FDEF38F8F7,007071CFBE381CE7038DC70039CE001C06C39CFB9C0E00381C738DCF,007071CFFE3F1CE7039DC70039EFE01C0EE39CFF9FCFC03F9EF39DC7C,007071CIE3F1FC7039CC70039EFE03C0EE3F8FF9FC7E03F9FE39CC3F,007071CE6E381DC7039FE70039CE001C1FF3B8EF9C00E0381EE39FE07,007939CE0E381CE793BFE7E03BCF001E5FF39CE79E08E0381CE3BFEC7,003F3F8E0E7F9CE3F3B8F7E03F8FF00FFC739CE79FEFE0381CF3B8FFE,,:::::M01FC7F9C01E78FC7FBIFE03F801EI01FC7F807E071C,M01DE781C01F31CE73DFCE0039803FI01DE7800F2073C,M01CE701C01F338E71C70E00380037I01CE7I0E00738,M01CF7F1C01FF38E73870FE03F0033I01CE7F01E003B8,M01CF7F1C01FF38E7F870FE01F80738001CE7F01E003B8,M01CE701C01CF38E73870EI03807F8001CE7I0E003F,M01FE7F9F81CF1DE73870FE0339CFF8E01FE7F80F671F1C,M01FC7F9F81E78FC73C70FE03F9EE1DE01FC7F807E71F3C,gR0CI0CP060018,^FS\n";
            s += "^FX Primera Seccion Producto\n";
            s += "^CF0,20\n";
            //s += "^FO30,30^FDFECHA ELABORACION:" + txtProduccion + " ^FS\n";
            s += "^FO350,30^FDLote:" + txtLote + "^FS\n";
            s += "^FO500,30^FDFECHA CADUCIDAD: " + txtCaducidad + "^FS\n";
            s += "^FO30,50^GB730,1,3^FS\n";
            s += "^BY2,2.5,60\n";
            s += "^FO560,70\n";
            s += "^CF0,20\n";
            s += "^FO350,70^FDPRODUCTO^FS\n";
            s += "^CF0,70\n";
            s += "^FO350,100^FD" + txtProducto + "^FS\n";
            s += "^CF0,45\n";
            s += "^FO50,190^FD" + txtDescripcion + "^FS\n";
            s += "^FO40,180^FR^GB730,60,60^FS\n";
            s += "^BY2,2.5,60\n";
            s += "^CF0,30\n";
            s += "^FO50,310^FDGTIN:" + gtin + "^FS\n";
            s += "^FO50,350^FDFecha de Ingreso: " + DateTime.Now.ToString("yyMMdd") + "^FS\n";
            s += "^FO550,350^FDPeso / Weigth^FS\n";
            s += "^CF0,60\n";
            s += "^FO550,300^FD" + peso + "KG.^FS\n";
            s += "^CF0,30\n";
            s += "^BY1,2.5,60\n";
            s += "^FO100,400\n";
            s += "^BCN,80,N,N,N,N\n";
            s += "^FD01" + gtin + "3102" + PesoRelleno + "11" + txtCaducidad + "21" + txtSecuencia + "^FS\n";
            s += "^FT120,510^A0N,20,20^FD(01)" + gtin + "(3102)" + PesoRelleno + "(11)" + txtCaducidad + "(21)" + txtSecuencia + "^FS\n";
            s += "^CF0,15\n";
            s += "^FO50,550^FDCOMERCIAL DE CARNES FRIAS DEL NORTE S.A.DE C.V^FS\n";
            s += "^FO50,570^FDAV.BRASIL 2800, ALAMITOS, 21210, MEXICALI, B.C.MX.^FS\n";
            s += "^FO600,570^FDWWW.CCFN.COM.MX^FS\n";
            s += "^FO30,540^GB730,1,3^FS\n";
            s += "^XZ\n";

            Console.WriteLine(value.IDPrinter);
            var bytes = Encoding.ASCII.GetBytes(s);
            RawPrinterHelper.SendBytesToPrinter("\\\\192.168.0.10\\" + value.IDPrinter, bytes, bytes.Length);
        }

        // POST: api/Impresion/Consumo
        [HttpPost("Consumo")]
        public void Consumo([FromBody] CarnesPrint value)
        {

            string txtProducto = value.ItemCode;
            string txtDescripcion = value.ItemName;
            string txtLote = value.Batch;
            string txtCaducidad = value.ExpirationDate;
            string txtPeso = value.Count;

            string txtSerie = "56";
            string txtSecuencia = txtSerie.PadLeft(14, '0');
            string gtin = txtProducto.PadLeft(14, '0');
            decimal peso = 0;
            string PesoRelleno = "000000";

            if (txtPeso.Length > 0)
            {
                peso = Convert.ToDecimal(txtPeso);
                PesoRelleno = txtPeso.Replace(".", "").PadLeft(6, '0');
            }

            string s = "^XA\n";
            s += "^FW\n";
            s += "^FO20,60^GFA,3000,3000,30,,T07gPF,T0gQFC,S03F8gN0FE,S07CgO01F,S0FgQ078,S0EgQ03C,R01C1gF8I01JFC1C,R03C3YFCK07IFE1E,R0387YFL01JF0E,R038YFEM07IF8E,R038YF8M03IF8E,R030YFN01IF87,R030XFCO07FF87,R030XF8O07FF87,R030XFP03FF87,R030UFCFEP01FF87,R030TFE0FCQ0FF87,R030TF00FCQ07F87,R030SFC00F8Q07F87,R030RFEI0FR03F87,R030RFJ0FR03F87,R030QF8J0EJ01FL03F87,R030PFEK0EJ0FFEK01F87,R030PFCK0CI03IFK01F87,R030PF8K0CI07IF8J01F87,R030PFL08I0JFCJ01F87,R030MFCFEL08001JFCJ01F87,R030LFE0FCK018003JFEJ01F87,R030LF00FCK0FI03JFEJ01F87,R030KFE00F8I01FFI07JFEJ01F87,R030KF800FI03IFI07JFEJ01F87,R030KFI0F001JFI07JFEJ01F87,R030JFCI0E007IFEI07JFEJ01F87,R030FFC7CI0E00JFEI0KFEJ01F87,R030FF078I0E01JFEI0KFEK0F87,R030FC07J0C01JFEI0KFEK0F87,R030F806J0C03JFEI0KFEK0F87,R030F006I0F803JFEI0KFEK0F87,R030E00400FF807FE3FEI0KFEK0F87,R030C00403FF807C03FEI0KFEK0F87,R030800407FF8J03FEI0KFEK0F87,R03080380IF8J03FEI0KFEK0F87,R03I0F81IF8J03FEI0KFEK0F87,R03003F81IF8J03FEI0KFEK0F87,R03007F81IF8J03FEI0KFEK0F87,R0300FF83IF8J03FEI0KFEK0787,U0FF03IF8J03FEI0KFEK07C,T01FF03IF8J03FEI0KFEK07C,::T01FF83IF8I07FFEI0KFEK07C,T01FF81IF80KFEI0KFEK07C,U0FF807C180KFEI0KFEK07C,U0FF8I0180KFEI0KFEK07C,W08I0180KFEI0KFEK07C,W0CI0180KFEI0KFEK07C,:W0CI0180KFEI0KFEK03C,W0EI0180KFEI0KFEK03C,:,:::::::::::::hQ01C,,001F1F8E0E3F9FC3F38787003F8FF00FC7C3F8E39FE7E03FDFE38787E,003F3BCF1E3F9DE7B38F87003FCFF01EC7C3BCF39FCF603FDEF38F8F7,007071CFBE381CE7038DC70039CE001C06C39CFB9C0E00381C738DCF,007071CFFE3F1CE7039DC70039EFE01C0EE39CFF9FCFC03F9EF39DC7C,007071CIE3F1FC7039CC70039EFE03C0EE3F8FF9FC7E03F9FE39CC3F,007071CE6E381DC7039FE70039CE001C1FF3B8EF9C00E0381EE39FE07,007939CE0E381CE793BFE7E03BCF001E5FF39CE79E08E0381CE3BFEC7,003F3F8E0E7F9CE3F3B8F7E03F8FF00FFC739CE79FEFE0381CF3B8FFE,,:::::M01FC7F9C01E78FC7FBIFE03F801EI01FC7F807E071C,M01DE781C01F31CE73DFCE0039803FI01DE7800F2073C,M01CE701C01F338E71C70E00380037I01CE7I0E00738,M01CF7F1C01FF38E73870FE03F0033I01CE7F01E003B8,M01CF7F1C01FF38E7F870FE01F80738001CE7F01E003B8,M01CE701C01CF38E73870EI03807F8001CE7I0E003F,M01FE7F9F81CF1DE73870FE0339CFF8E01FE7F80F671F1C,M01FC7F9F81E78FC73C70FE03F9EE1DE01FC7F807E71F3C,gR0CI0CP060018,^FS\n";
            s += "^FX Primera Seccion Producto\n";
            s += "^CF0,20\n";
            s += "^FO350,30^FDLote:" + txtLote + "^FS\n";
            s += "^FO500,30^FDFECHA CADUCIDAD: " + txtCaducidad + "^FS\n";
            s += "^FO30,50^GB730,1,3^FS\n";
            s += "^BY2,2.5,60\n";
            s += "^FO560,70\n";
            s += "^CF0,20\n";
            s += "^FO350,70^FDPRODUCTO^FS\n";
            s += "^CF0,70\n";
            s += "^FO350,100^FD" + txtProducto + "^FS\n";
            s += "^CF0,45\n";
            s += "^FO50,190^FD" + txtDescripcion + "^FS\n";
            s += "^FO40,180^FR^GB730,60,60^FS\n";
            s += "^BY2,2.5,60\n";
            s += "^CF0,30\n";
            s += "^FO50,310^FDGTIN:" + gtin + "^FS\n";
            s += "^FO50,350^FDFecha de Ingreso: " + DateTime.Now.ToString("yyMMdd") + "^FS\n";
            s += "^FO550,350^FDPeso / Weigth^FS\n";
            s += "^CF0,60\n";
            s += "^FO550,300^FD" + peso + "KG.^FS\n";
            s += "^CF0,30\n";
            s += "^BY1,2.5,60\n";
            s += "^FO100,400\n";
            s += "^BCN,80,N,N,N,N\n";
            s += "^FD" + gtin + "^FS\n";
            s += "^FT120,510^A0N,20,20^FD" + gtin + "^FS\n";
            s += "^CF0,15\n";
            s += "^FO50,550^FDCOMERCIAL DE CARNES FRIAS DEL NORTE S.A.DE C.V^FS\n";
            s += "^FO50,570^FDAV.BRASIL 2800, ALAMITOS, 21210, MEXICALI, B.C.MX.^FS\n";
            s += "^FO600,570^FDWWW.CCFN.COM.MX^FS\n";
            s += "^FO30,540^GB730,1,3^FS\n";
            s += "^XZ\n";

            var bytes = Encoding.ASCII.GetBytes(s);
            RawPrinterHelper.SendBytesToPrinter("\\\\192.168.0.10\\" + value.IDPrinter, bytes, bytes.Length);
        }


        // POST: api/Impresion/MasterDomicilio
        [HttpPost("MasterDomicilio")]
        public void MasterDomicilio([FromBody] MDomicilioPrint value)
        {

            string txtCuarto = value.Room;
            string txtZona = value.Zone;
            string txtPasillo = value.Pasillo;
            string txtRack = value.Rack;
            string s = "^XA\n";
            s += "^FW\n";
            s += "^FO20,60^GFA,3000,3000,30,,T07gPF,T0gQFC,S03F8gN0FE,S07CgO01F,S0FgQ078,S0EgQ03C,R01C1gF8I01JFC1C,R03C3YFCK07IFE1E,R0387YFL01JF0E,R038YFEM07IF8E,R038YF8M03IF8E,R030YFN01IF87,R030XFCO07FF87,R030XF8O07FF87,R030XFP03FF87,R030UFCFEP01FF87,R030TFE0FCQ0FF87,R030TF00FCQ07F87,R030SFC00F8Q07F87,R030RFEI0FR03F87,R030RFJ0FR03F87,R030QF8J0EJ01FL03F87,R030PFEK0EJ0FFEK01F87,R030PFCK0CI03IFK01F87,R030PF8K0CI07IF8J01F87,R030PFL08I0JFCJ01F87,R030MFCFEL08001JFCJ01F87,R030LFE0FCK018003JFEJ01F87,R030LF00FCK0FI03JFEJ01F87,R030KFE00F8I01FFI07JFEJ01F87,R030KF800FI03IFI07JFEJ01F87,R030KFI0F001JFI07JFEJ01F87,R030JFCI0E007IFEI07JFEJ01F87,R030FFC7CI0E00JFEI0KFEJ01F87,R030FF078I0E01JFEI0KFEK0F87,R030FC07J0C01JFEI0KFEK0F87,R030F806J0C03JFEI0KFEK0F87,R030F006I0F803JFEI0KFEK0F87,R030E00400FF807FE3FEI0KFEK0F87,R030C00403FF807C03FEI0KFEK0F87,R030800407FF8J03FEI0KFEK0F87,R03080380IF8J03FEI0KFEK0F87,R03I0F81IF8J03FEI0KFEK0F87,R03003F81IF8J03FEI0KFEK0F87,R03007F81IF8J03FEI0KFEK0F87,R0300FF83IF8J03FEI0KFEK0787,U0FF03IF8J03FEI0KFEK07C,T01FF03IF8J03FEI0KFEK07C,::T01FF83IF8I07FFEI0KFEK07C,T01FF81IF80KFEI0KFEK07C,U0FF807C180KFEI0KFEK07C,U0FF8I0180KFEI0KFEK07C,W08I0180KFEI0KFEK07C,W0CI0180KFEI0KFEK07C,:W0CI0180KFEI0KFEK03C,W0EI0180KFEI0KFEK03C,:,:::::::::::::hQ01C,,001F1F8E0E3F9FC3F38787003F8FF00FC7C3F8E39FE7E03FDFE38787E,003F3BCF1E3F9DE7B38F87003FCFF01EC7C3BCF39FCF603FDEF38F8F7,007071CFBE381CE7038DC70039CE001C06C39CFB9C0E00381C738DCF,007071CFFE3F1CE7039DC70039EFE01C0EE39CFF9FCFC03F9EF39DC7C,007071CIE3F1FC7039CC70039EFE03C0EE3F8FF9FC7E03F9FE39CC3F,007071CE6E381DC7039FE70039CE001C1FF3B8EF9C00E0381EE39FE07,007939CE0E381CE793BFE7E03BCF001E5FF39CE79E08E0381CE3BFEC7,003F3F8E0E7F9CE3F3B8F7E03F8FF00FFC739CE79FEFE0381CF3B8FFE,,:::::M01FC7F9C01E78FC7FBIFE03F801EI01FC7F807E071C,M01DE781C01F31CE73DFCE0039803FI01DE7800F2073C,M01CE701C01F338E71C70E00380037I01CE7I0E00738,M01CF7F1C01FF38E73870FE03F0033I01CE7F01E003B8,M01CF7F1C01FF38E7F870FE01F80738001CE7F01E003B8,M01CE701C01CF38E73870EI03807F8001CE7I0E003F,M01FE7F9F81CF1DE73870FE0339CFF8E01FE7F80F671F1C,M01FC7F9F81E78FC73C70FE03F9EE1DE01FC7F807E71F3C,gR0CI0CP060018,^FS\n";
            s += "^FX Primera Seccion Producto\n";
            s += "^CF0,20\n";
            //s += "^FO30,30^FDFECHA ELABORACION:" + txtProduccion + " ^FS\n";
            s += "^FO30,50^GB730,1,3^FS\n";
            s += "^BY2,2.5,60\n";
            s += "^FO560,70\n";
            s += "^CF0,20\n";
            s += "^FO350,70^FDCUARTO^FS\n";
            s += "^CF0,70\n";
            s += "^FO350,100^FDCARNES - CC^FS\n";
            s += "^CF0,45\n";
            s += "^FO50,190^FDZONA:CC PASILLO:PJ RACK:01  ^FS\n";
            s += "^FO40,180^FR^GB730,60,60^FS\n";
            s += "^BY2,2.5,60\n";
            s += "^CF0,30\n";
            s += "^FO100,320^FDZONA:" + txtZona + "^FS\n";
            s += "^FO350,320^FDPASILLO:" + txtPasillo + "^FS\n";
            s += "^FO600,320^FDRACK:" + txtRack + "^FS\n";
            s += "^CF0,30\n";
            s += "^BY1,2.5,60\n";
            s += "^FO350,400\n";
            s += "^BCN,80,N,N,N,N\n";
            s += "^FD" + txtZona + txtPasillo + txtRack + "^FS\n";
            s += "^FT370,500^A0N,20,20^FD" + txtZona + "-" + txtPasillo + "-" + txtRack + "^FS\n";
            s += "^CF0,15\n";
            s += "^FO50,550^FDCOMERCIAL DE CARNES FRIAS DEL NORTE S.A.DE C.V^FS\n";
            s += "^FO50,570^FDAV.BRASIL 2800, ALAMITOS, 21210, MEXICALI, B.C.MX.^FS\n";
            s += "^FO600,570^FDWWW.CCFN.COM.MX^FS\n";
            s += "^FO30,540^GB730,1,3^FS\n";
            s += "^XZ\n";

            var bytes = Encoding.ASCII.GetBytes(s);
            RawPrinterHelper.SendBytesToPrinter("\\\\192.168.0.10\\" + value.IDPrinter, bytes, bytes.Length);
        }



        // POST: api/Impresion/MasterDomicilio
        [HttpPost("Domicilio")]
        public void Domicilio([FromBody] DomicilioPrint value)
        {

            string txtCuarto = value.Room;
            string txtZona = value.Zone;
            string txtPasillo = value.Pasillo;
            string txtSeccion = value.Section;
            string txtNivel = value.Nivel;
            string txtPosicion = value.Position;

            string s = "^XA\n";
            s += "^FW\n";
            s += "^FO10,20^GFA,3000,3000,30,,T07gPF,T0gQFC,S03F8gN0FE,S07CgO01F,S0FgQ078,S0EgQ03C,R01C1gF8I01JFC1C,R03C3YFCK07IFE1E,R0387YFL01JF0E,R038YFEM07IF8E,R038YF8M03IF8E,R030YFN01IF87,R030XFCO07FF87,R030XF8O07FF87,R030XFP03FF87,R030UFCFEP01FF87,R030TFE0FCQ0FF87,R030TF00FCQ07F87,R030SFC00F8Q07F87,R030RFEI0FR03F87,R030RFJ0FR03F87,R030QF8J0EJ01FL03F87,R030PFEK0EJ0FFEK01F87,R030PFCK0CI03IFK01F87,R030PF8K0CI07IF8J01F87,R030PFL08I0JFCJ01F87,R030MFCFEL08001JFCJ01F87,R030LFE0FCK018003JFEJ01F87,R030LF00FCK0FI03JFEJ01F87,R030KFE00F8I01FFI07JFEJ01F87,R030KF800FI03IFI07JFEJ01F87,R030KFI0F001JFI07JFEJ01F87,R030JFCI0E007IFEI07JFEJ01F87,R030FFC7CI0E00JFEI0KFEJ01F87,R030FF078I0E01JFEI0KFEK0F87,R030FC07J0C01JFEI0KFEK0F87,R030F806J0C03JFEI0KFEK0F87,R030F006I0F803JFEI0KFEK0F87,R030E00400FF807FE3FEI0KFEK0F87,R030C00403FF807C03FEI0KFEK0F87,R030800407FF8J03FEI0KFEK0F87,R03080380IF8J03FEI0KFEK0F87,R03I0F81IF8J03FEI0KFEK0F87,R03003F81IF8J03FEI0KFEK0F87,R03007F81IF8J03FEI0KFEK0F87,R0300FF83IF8J03FEI0KFEK0787,U0FF03IF8J03FEI0KFEK07C,T01FF03IF8J03FEI0KFEK07C,::T01FF83IF8I07FFEI0KFEK07C,T01FF81IF80KFEI0KFEK07C,U0FF807C180KFEI0KFEK07C,U0FF8I0180KFEI0KFEK07C,W08I0180KFEI0KFEK07C,W0CI0180KFEI0KFEK07C,:W0CI0180KFEI0KFEK03C,W0EI0180KFEI0KFEK03C,:,:::::::::::::hQ01C,,001F1F8E0E3F9FC3F38787003F8FF00FC7C3F8E39FE7E03FDFE38787E,003F3BCF1E3F9DE7B38F87003FCFF01EC7C3BCF39FCF603FDEF38F8F7,007071CFBE381CE7038DC70039CE001C06C39CFB9C0E00381C738DCF,007071CFFE3F1CE7039DC70039EFE01C0EE39CFF9FCFC03F9EF39DC7C,007071CIE3F1FC7039CC70039EFE03C0EE3F8FF9FC7E03F9FE39CC3F,007071CE6E381DC7039FE70039CE001C1FF3B8EF9C00E0381EE39FE07,007939CE0E381CE793BFE7E03BCF001E5FF39CE79E08E0381CE3BFEC7,003F3F8E0E7F9CE3F3B8F7E03F8FF00FFC739CE79FEFE0381CF3B8FFE,,:::::M01FC7F9C01E78FC7FBIFE03F801EI01FC7F807E071C,M01DE781C01F31CE73DFCE0039803FI01DE7800F2073C,M01CE701C01F338E71C70E00380037I01CE7I0E00738,M01CF7F1C01FF38E73870FE03F0033I01CE7F01E003B8,M01CF7F1C01FF38E7F870FE01F80738001CE7F01E003B8,M01CE701C01CF38E73870EI03807F8001CE7I0E003F,M01FE7F9F81CF1DE73870FE0339CFF8E01FE7F80F671F1C,M01FC7F9F81E78FC73C70FE03F9EE1DE01FC7F807E71F3C,gR0CI0CP060018,^FS\n";
            s += "^FX Primera Seccion Producto\n";
            s += "^CF0,20\n";
            s += "^FO300,50^FDCUARTO^FS\n";
            s += "^CF0,30\n";
            s += "^FO300,70^FD" + txtCuarto + "^FS\n";
            s += "^CF0,25\n";
            s += "^FO30,160^FDPASILLO:" + txtPasillo + " SECCION:" + txtSeccion + " NIVEL: " + txtNivel + " POSICION:" + txtPosicion + "^FS\n";
            s += "^FO20,150^FR^GB580,50,50^FS\n";
            s += "^BY2,2.5,60\n";
            s += "^CF0,20\n";
            s += "^FO80,210^FDZONA:" + txtZona + "^FS\n";
            s += "^FO250,210^FDPASILLO:" + txtPasillo + "^FS\n";
            s += "^FO420,210^FDSECCION:" + txtSeccion + "^FS\n";
            s += "^FO80,250^FDNIVEL:" + txtNivel + "^FS\n";
            s += "^FO420,250^FDPOSICION:" + txtPosicion + "^FS\n";
            s += "^CF0,40\n";
            s += "^BY1,2.5,60\n";
            s += "^FO240,270\n";
            s += "^BCN,80,N,N,N,N\n";
            s += "^FD" + txtPasillo + txtSeccion + txtNivel + txtPosicion + "^FS\n";
            s += "^FT235,370^A0N,20,20^FD" + txtPasillo + "-" + txtSeccion + "-" + txtNivel + "-" + txtPosicion + "^FS\n";
            s += "^CF0,10\n";
            s += "^FO50,430^FDCOMERCIAL DE CARNES FRIAS DEL NORTE S.A.DE C.V^FS\n";
            s += "^FO50,440^FDAV.BRASIL 2800, ALAMITOS, 21210, MEXICALI, B.C.MX.^FS\n";
            s += "^FO400,440^FDWWW.CCFN.COM.MX^FS\n";
            s += "^FO30,420^GB730,1,3^FS\n";
            s += "^XZ\n";

            var bytes = Encoding.ASCII.GetBytes(s);
            RawPrinterHelper.SendBytesToPrinter("\\\\192.168.0.10\\" + value.IDPrinter, bytes, bytes.Length);
        }
        public class CarnesPrint
        {
            public string ItemCode { set; get; }
            public string ItemName { set; get; }
            public string Batch { set; get; }
            public string ExpirationDate { set; get; }
            public string Count { set; get; }
            public string IDPrinter { set; get; }
        }
        public class DomicilioPrint
        {
            public string Room { set; get; }
            public string Zone { set; get; }
            public string Pasillo { set; get; }
            public string Position { set; get; }
            public string Section { set; get; }
            public string Nivel { set; get; }
            public string IDPrinter { set; get; }
        }
        public class MDomicilioPrint
        {
            public string Room { set; get; }
            public string Zone { set; get; }
            public string Pasillo { set; get; }
            public string Rack { set; get; }
            public string IDPrinter { set; get; }
        }
    }


}
