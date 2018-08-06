using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using Limena_surveys.Models;
using System.Web.Script.Serialization;
using System.Net;
using System.Xml.Linq;
using CrystalDecisions.CrystalReports.Engine;
using System.IO;

namespace Limena_surveys.Controllers
{
    public class HomeController : Controller
    {
        private DLI_SurveyEntities db = new DLI_SurveyEntities();
        private DLI_PROEntities DLIPRO = new DLI_PROEntities();



        public ActionResult Index()
        {
            if (Session["idSAP_Vendedor"] == null)
            {

                return View();

            }
            else
            {
                try
                {


                    int ID_customer = 0;
                    ID_customer = Convert.ToInt32(Session["idSAP_Vendedor"]);

                    var clientes = (from b in DLIPRO.OCRDs where (b.SlpCode == ID_customer && b.CardType == "C" && b.validFor == "Y" && b.CardCode != "F%" && b.CardCode != "41%" && b.CardCode != "31%") select new lsClientes { Value = b.CardCode, Text = b.CardName, enabled = 1 }).OrderBy(b => b.Text).ToList();



                    foreach (var item in clientes)
                    {

                        item.Text = item.Value.ToString() + " - " + item.Text.ToString();

                        var existe = (from c in db.Detalle_encuesta where (c.idSAP_cliente == item.Value.ToString()) select c).ToList();

                        if (existe.Count > 0)
                        {
                            item.enabled = 0;
                        }
                        else
                        {

                            item.enabled = 1;
                        }

                        existe = null;
                    }




                    ViewBag.clientes = clientes.ToList();
                    return View();


                }
                catch (Exception ex)
                {
                    return View();


                }
            }

            
        }
        public class lsClientes_cpanel
        {
            public string Value { get; set; }
            public string Text { get; set; }
            public int pendiente { get; set; }

        }


        public class lsClientes_topfive
        {
            public string IDSAP { get; set; }
            public string Vendedor { get; set; }
            public int count { get; set; }

        }

        public class lsClientes_stats
        {
            public string IDSAP { get; set; }
            public string Vendedor { get; set; }
            public int countF { get; set; }
            public int countP { get; set; }
            public int countT { get; set; }
        }

        public ActionResult Cpanel()
        {
            if (Session["administrador"] == null)
            {
                return RedirectToAction("Index");
            }
            else
            {

                //SECCION Tarjetas de estado
                var nclientes = (from b in DLIPRO.OCRDs where (b.CardType == "C" && b.validFor == "Y" && b.CardCode != "F%" && b.CardCode != "41%" && b.CardCode != "31%") select b).ToList();
                if (nclientes == null)
                {
                    ViewBag.totalCli = 0;
                    ViewBag.clientesE = 0;
                    ViewBag.clientesP =  0;
                }
                else {
                    var nclientesE = (from c in db.Clientes where (c.estado == 1) select c).ToList();
                    int cliE = 0;
                    if (nclientesE == null)
                    {
                        cliE = 0;
                    }
                    else {
                        cliE = nclientesE.Count();
                    }

                    ViewBag.totalCli = nclientes.Count();
                    ViewBag.clientesE = cliE;
                    ViewBag.clientesP = nclientes.Count() - cliE;
                }

                int nCerrado = (from i in db.Clientes where (i.estado == 2) select i).Count();
                int nNoVisita = (from i in db.Clientes where (i.estado == 3) select i).Count();

                
                ViewBag.clienteCe = nCerrado;
                ViewBag.clienteNoVisita = nNoVisita;

                ViewBag.clientesEg2 = ViewBag.clientesE + nCerrado + nNoVisita;
                ViewBag.clientesP = ViewBag.clientesP - nCerrado - nNoVisita;
                // FIN SECCION

                //SECCION Graficos

                var listaVendedores = (from d in db.Vendedores select d).OrderBy(d => d.nomSAP_vendedor).ToList();
                //TOP 5 vendedores

                var Top5Query = from p in db.Clientes.GroupBy(p => p.idSAP_vendedor)
                                select new lsClientes_topfive
                            {
                                IDSAP = p.FirstOrDefault().idSAP_vendedor,
                                Vendedor = p.FirstOrDefault().nomSAP_vendedor,
                                count = p.Count()
                                
                            };

                ViewBag.Topfive = Top5Query.OrderByDescending(x=> x.count).Take(5).ToList();
                //Grafico por vendedor
                var listIDVendedores = String.Join(",", listaVendedores.Select(o => "\"" + o.nomSAP_vendedor.ToString() + "\"").ToArray());
                ViewBag.listIDV = listIDVendedores;


                var statsQuery = from h in listaVendedores select new lsClientes_stats
                {
                    IDSAP = h.idSAP_vendedor,
                    Vendedor = h.nomSAP_vendedor,
                    countF = 0,
                    countP = 0
                };

                statsQuery = statsQuery.ToList();

                foreach (var stats in statsQuery) {
                    int id = Convert.ToInt32(stats.IDSAP);
                    int countTotal = (from f in DLIPRO.OCRDs where (f.SlpCode == id && f.CardType == "C" && f.validFor == "Y" && f.CardCode != "F%" && f.CardCode != "41%" && f.CardCode != "31%") select f).Count();
                    int finalizadas = (from g in db.Clientes where (g.idSAP_vendedor == stats.IDSAP) select g).Count();

                    stats.countF = finalizadas ;
                    stats.countP = countTotal - finalizadas;
                    stats.countT = countTotal;

                }

                ViewBag.FinalizadasGrafico = String.Join(",", statsQuery.Select(o => o.countF.ToString()).ToArray());
                ViewBag.PendientesGrafico = String.Join(",", statsQuery.Select(o => o.countP.ToString()).ToArray());
                ViewBag.TotalGrafico = String.Join(",", statsQuery.Select(o => o.countT.ToString()).ToArray());
                //lista de vendedores

                //porcentaje
                foreach (var item in listaVendedores) {
                    int id = Convert.ToInt32(item.idSAP_vendedor);
                    int countTotal  = (from f in DLIPRO.OCRDs where (f.SlpCode == id && f.CardType == "C" && f.validFor == "Y" && f.CardCode != "F%" && f.CardCode != "41%" && f.CardCode != "31%") select f).Count();
                    int finalizadas = (from g in db.Clientes where (g.idSAP_vendedor == item.idSAP_vendedor) select g).Count();

                    decimal total = 0;
                    try
                    {
                        total = Convert.ToInt32((finalizadas * 100)/countTotal);
                    }
                    catch {
                        total = 0;
                    }

                    item.ID_vendedor = Convert.ToInt32(total);

                }


                ViewBag.listaV = listaVendedores.ToList();
                //FIN SECCION


                return View();


            }

        }


        public ActionResult Encuestas(int? id)
        {
            if (Session["administrador"] == null)
            {
                return RedirectToAction("Index");
            }
            else
            {

                if (id == null)
                {
                    //var clientes = (from b in DLIPRO.OCRDs where (b.CardType == "C" && b.validFor == "Y" && b.CardCode != "F%" && b.CardCode != "41%" && b.CardCode != "31%") select new lsClientes_cpanel { Value = b.CardCode, Text = b.CardName, pendiente = 1 }).OrderBy(b => b.Text).ToList();

                    //foreach (var item in clientes)
                    //{

                    //    var existe = (from c in db.Detalle_encuesta where (c.idSAP_cliente == item.Value.ToString()) select c).ToList();

                    //    if (existe.Count > 0)
                    //    {
                    //        item.pendiente = 0;
                    //    }
                    //    else
                    //    {

                    //        item.pendiente = 1;
                    //    }

                    //    existe = null;
                    //}
                    //ViewBag.vendedor = "";
                    //ViewBag.lista = clientes;
                    //ViewBag.vendedores = (from v in db.Vendedores select v).ToList();
                    return RedirectToAction("Cpanel");
                }
                else
                {
                    var clientes = (from b in DLIPRO.OCRDs where (b.SlpCode == id && b.CardType == "C" && b.validFor == "Y" && b.CardCode != "F%" && b.CardCode != "41%" && b.CardCode != "31%") select new lsClientes_cpanel { Value = b.CardCode, Text = b.CardName, pendiente = 1 }).OrderBy(b => b.Text).ToList();

                    foreach (var item in clientes)
                    {

                        var existe = (from c in db.Detalle_encuesta where (c.idSAP_cliente == item.Value.ToString()) select c).ToList();

                        if (existe.Count > 0)
                        {
                            item.pendiente = 0;
                            //Validamos caso especial
                            var existecasoEspecialCerrado = (from j in existe where (j.ID_item == "c2") select j).ToList();

                            if (existecasoEspecialCerrado.Count > 0) {
                                item.pendiente =2;
                            }

                            var existecasoEspecialNovisita = (from k in existe where (k.ID_item == "c3") select k).ToList();

                            if (existecasoEspecialNovisita.Count > 0)
                            {
                                item.pendiente = 3;
                            }

                        }
                        else
                        {

                            item.pendiente = 1;
                        }

                        existe = null;
                    }

                    var vendedor = (from v in db.Vendedores where (v.idSAP_vendedor == id.ToString()) select v).FirstOrDefault();
                    if (vendedor == null)
                    {
                        ViewBag.nomvendedor = "";
                    }
                    else
                    {
                        ViewBag.nomvendedor = vendedor.nomSAP_vendedor;
                    }

                  
                    int countTotal = (from f in DLIPRO.OCRDs where (f.SlpCode == id && f.CardType == "C" && f.validFor == "Y" && f.CardCode != "F%" && f.CardCode != "41%" && f.CardCode != "31%") select f).Count();
                    int finalizadas = (from g in db.Clientes where (g.idSAP_vendedor == id.ToString() && g.estado==1) select g).Count();

                    ViewBag.totalCli = countTotal;
                    ViewBag.clientesE = finalizadas;
                    

                    int nCerrado = (from i in db.Clientes where (i.estado == 2 && i.idSAP_vendedor==id.ToString()) select i).Count();
                    int nNoVisita = (from i in db.Clientes where (i.estado == 3 && i.idSAP_vendedor == id.ToString()) select i).Count();

                    ViewBag.clienteCe = nCerrado;
                    ViewBag.clienteNoVisita = nNoVisita;

                    ViewBag.clientesP = countTotal - finalizadas - nCerrado - nNoVisita;

                    ViewBag.lista = clientes;
                    //ViewBag.vendedores = (from v in db.Vendedores select v).ToList();
                    return View();

                }



            }


            //List<lsClientes_cpanel> selectList_clientes = new List<lsClientes_cpanel>();

            //foreach (var item in clientes)
            //{
            //    lsClientes_cpanel nuevoc = new lsClientes_cpanel();

            //    nuevoc.Value = item.CardCode.ToString();
            //    nuevoc.Text = item.CardName.ToString();

            //    var existe = (from c in db.Detalle_encuesta where (c.idSAP_cliente == item.CardCode.ToString()) select c).ToList();

            //    if (existe.Count > 0)
            //    {
            //        nuevoc.pendiente = 0;
            //    }
            //    else
            //    {

            //        nuevoc.pendiente = 1;
            //    }

            //    selectList_clientes.Add(nuevoc);

            //    existe = null;
            //}

        }


        public ActionResult ImpEncuestas(string id)
        {
            if (Session["administrador"] == null)
            {
                return RedirectToAction("Index");
            }
            else
            {

                if (id == null)
                {

                    return RedirectToAction("Cpanel");
                }
                else
                {
                    var cliente = (from a in db.Clientes where (a.idSAP_cliente == id.ToString()) select a).ToList();

                    ReportDocument rd = new ReportDocument();

                    rd.Load(Path.Combine(Server.MapPath("~/Reportes"), "rptEncuesta01.rpt"));

                    rd.SetDataSource(cliente);

                    //rd.Subreports[0].SetDataSource(result);
                    //var filePathOriginal = Server.MapPath("/Reportes/pdf");
                    var detalles = (from k in db.Detalle_encuesta where (k.idSAP_cliente == id.ToString()) select k).ToList();

                    foreach (var item in detalles) {
                        if (item.ID_item == "chk1") {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk1", "X"); } else { rd.SetParameterValue("chk1", ""); }                            
                        }
                        if (item.ID_item == "chk2")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk2", "X"); } else { rd.SetParameterValue("chk2", ""); }
                        }
                        if (item.ID_item == "chk3")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk3", "X"); } else { rd.SetParameterValue("chk3", ""); }
                        }
                        if (item.ID_item == "chk4")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk4", "X"); } else { rd.SetParameterValue("chk4", ""); }
                        }
                        if (item.ID_item == "chk5")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk5", "X"); } else { rd.SetParameterValue("chk5", ""); }
                        }
                        if (item.ID_item == "chk6")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk6", "X"); } else { rd.SetParameterValue("chk6", ""); }
                        }
                        if (item.ID_item == "chk7")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk7", "X"); } else { rd.SetParameterValue("chk7", ""); }
                        }
                        if (item.ID_item == "chk8")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk8", "X"); } else { rd.SetParameterValue("chk8", ""); }
                        }
                        if (item.ID_item == "chk9")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk9", "X"); } else { rd.SetParameterValue("chk9", ""); }
                        }
                        if (item.ID_item == "chk10")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk10", "X"); } else { rd.SetParameterValue("chk10", ""); }
                        }
                        if (item.ID_item == "chk11")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk11", "X"); } else { rd.SetParameterValue("chk11", ""); }
                        }
                        if (item.ID_item == "chk12")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk12", "X"); } else { rd.SetParameterValue("chk12", ""); }
                        }
                        if (item.ID_item == "chk13")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk13", "X"); } else { rd.SetParameterValue("chk13", ""); }
                        }
                        if (item.ID_item == "chk14")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk14", "X"); } else { rd.SetParameterValue("chk14", ""); }
                        }
                        if (item.ID_item == "chk15")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk15", "X"); } else { rd.SetParameterValue("chk15", ""); }
                        }
                        if (item.ID_item == "chk16")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk16", "X"); } else { rd.SetParameterValue("chk16", ""); }
                        }
                        if (item.ID_item == "chk17")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk17", "X"); } else { rd.SetParameterValue("chk17", ""); }
                        }
                        if (item.ID_item == "chk18")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk18", "X"); } else { rd.SetParameterValue("chk18", ""); }
                        }
                        if (item.ID_item == "chk19")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk19", "X"); } else { rd.SetParameterValue("chk19", ""); }
                        }
                        if (item.ID_item == "chk20")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk20", "X"); } else { rd.SetParameterValue("chk20", ""); }
                        }
                        if (item.ID_item == "chk21")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk21", "X"); } else { rd.SetParameterValue("chk21", ""); }
                        }
                        if (item.ID_item == "chk22")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk22", "X"); } else { rd.SetParameterValue("chk22", ""); }
                        }
                        if (item.ID_item == "chk23")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk23", "X"); } else { rd.SetParameterValue("chk23", ""); }
                        }
                        if (item.ID_item == "chk23")
                        {
                            if (item.value_item_chk == true) { rd.SetParameterValue("chk23", "X"); } else { rd.SetParameterValue("chk23", ""); }
                        }
                        if (item.ID_item == "ss1" || item.ID_item == "ss2" || item.ID_item == "ss3" || item.ID_item == "ss4" || item.ID_item == "ss5")
                        {
                            rd.SetParameterValue("tamano", item.descripcion_item);
                        }
                    }
                    
                   

                    Response.Buffer = false;
                    Response.ClearContent();
                    Response.ClearHeaders();


                    //PARA VISUALIZAR
                    Response.AppendHeader("Content-Disposition", "inline; filename=" + "Encuesta; ");


                    Stream stream = rd.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);

                    stream.Seek(0, SeekOrigin.Begin);



                    return File(stream, System.Net.Mime.MediaTypeNames.Application.Pdf);

             

                }



            }

        }


        public class lsClientes
        {
            public string Value { get; set; }
            public string Text { get; set; }
            public int enabled { get; set; }

        }
        public ActionResult Iniciar_sesion(string usuariocorreo, string password)
        {
            //Validamos del lado del cliente que ambos parametros no vengan vacios

            //Validamos si es superusuario
            if (usuariocorreo == "admin@limenainc.net" && password == "@dmin2018") {

                Session["administrador"] = "activo";
        

                return Json(new { success = true, cpanel = "si" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
 
                    try
                    {
                        //var list = (from d in db.Vendedores select d).ToList();
                        var obj = (from a in db.Vendedores where (a.correo == usuariocorreo && a.contrasena == password) select a).FirstOrDefault();
                        if (obj != null)
                        {


                            Session["ID_vendedor"] = obj.ID_vendedor.ToString();
                            Session["idSAP_Vendedor"] = obj.idSAP_vendedor.ToString();
                            Session["nomSAP_Vendedor"] = obj.nomSAP_vendedor.ToString();
                        int ID_customer = 0;
                            ID_customer = Convert.ToInt32(obj.idSAP_vendedor);

                            var clientes = (from b in DLIPRO.OCRDs where (b.SlpCode == ID_customer && b.CardType == "C" && b.validFor == "Y" && b.CardCode != "F%" && b.CardCode != "41%" && b.CardCode != "31%") select new lsClientes { Value = b.CardCode, Text = b.CardName, enabled = 1 }).OrderBy(b => b.Text).ToList();



                            foreach (var item in clientes)
                            {

                                item.Text = item.Value.ToString() + " - " + item.Text.ToString();

                                var existe = (from c in db.Detalle_encuesta where (c.idSAP_cliente == item.Value.ToString()) select c).ToList();

                                if (existe.Count > 0)
                                {
                                    item.enabled = 0;
                                }
                                else
                                {

                                    item.enabled = 1;
                                }

                                existe = null;
                            }



                            ViewBag.clientes = null;
                            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
                            string result = javaScriptSerializer.Serialize(clientes.ToList());
                            return Json(new { success = true, lstclientes = result, cpanel = "no" }, JsonRequestBehavior.AllowGet);

                        }
                        else
                        {
                            //Si ingreso mal la contraseña o el usuario no existe
                            ViewBag.clientes = null;
                            return Json(new { success = false, cpanel = "no" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    catch (Exception ex)
                    {
                    ViewBag.clientes = null;
                    return Json(new { success = false, cpanel = "no" }, JsonRequestBehavior.AllowGet);


                    } 
           
            

            }
        }

        public class MyObj_formtemplate
        {
            public string id_item { get; set; }
            public int seccion { get; set; }
            public string seccion_descripcion { get; set; }
            public string descripcion_item { get; set; }
            public Boolean value_item_onlyforchk { get; set; }
            public string idSAP_cliente { get; set; }
        }

        public ActionResult Enviar_datos(List<MyObj_formtemplate> values)
        {
            var id_clienteSAP = "";

                if (values.Count > 0)
                {

                    foreach (var item in values)
                    {
                    Detalle_encuesta dtnuevo = new Detalle_encuesta();
                    id_clienteSAP = item.idSAP_cliente;//Variable global

                    dtnuevo.seccion = item.seccion;
                    dtnuevo.seccion_descripcion = item.seccion_descripcion;
                    dtnuevo.ID_item = item.id_item;
                    dtnuevo.descripcion_item = item.descripcion_item;
                    dtnuevo.value_item_chk = item.value_item_onlyforchk;
                    dtnuevo.idSAP_cliente = item.idSAP_cliente;

                    db.Detalle_encuesta.Add(dtnuevo);
                    db.SaveChanges();
                    }


                //Enviamos el correo al usuario
                //SendDemoResume(Convert.ToInt32(id_demo));

                //Guardamos la encuesta en tabla maestra

                var idSAPVen = Session["idSAP_Vendedor"].ToString();
                if (idSAPVen == null) { idSAPVen = "NO DISPONIBLE"; }
                var idnomVen = Session["nomSAP_Vendedor"].ToString();
                if (idnomVen== null) {idnomVen = "NO DISPONIBLE"; }
                try
                {

                    Clientes cli_nuevo = new Clientes();
                    //EVALUAMOS SI ES CASO ESPECIAL
                    
                    if (values.Count == 1)
                    {
                        if (values[0].id_item == "c2")
                        {
                            cli_nuevo.estado = 2; //Cerrado
                        }
                        else if (values[0].id_item == "c3")
                        {
                            cli_nuevo.estado = 3; //No se visita
                        }
                    }
                    else {
                        cli_nuevo.estado = 1; //Finalizado
                    }
                    var nomcliente = (from h in DLIPRO.OCRDs where (h.CardCode == id_clienteSAP) select h).FirstOrDefault();
                    if (nomcliente == null)
                    {
                        cli_nuevo.nomSAP_cliente = "";
                    }
                    else {
                        cli_nuevo.nomSAP_cliente = nomcliente.CardName;
                    }

                    cli_nuevo.idSAP_cliente = id_clienteSAP;
                    
                    cli_nuevo.idSAP_vendedor = idSAPVen;
                    cli_nuevo.nomSAP_vendedor = idnomVen;

                    db.Clientes.Add(cli_nuevo);
                    db.SaveChanges();

                    return Json(new { Result = "Success" });
                }
                catch (Exception ex) {
                    
                }




                    return Json(new { Result = "Success" });
                }
                return Json(new { Result = "Warning" });


        }
        //public class MyObjStores
        //{
        //    public string id { get; set; }
        //    public string store { get; set; }
        //    public string lng { get; set; }
        //    public string lat { get; set; }
        //}

        //public ActionResult Storeslatlong()
        //{



        //    var store = DLIPRO.OCRDs.Where(b => b.CardType == "C" && b.CardName != null && b.CardName != "").OrderBy(b => b.CardName).ToList();

        //    List<MyObjStores> listaTiendas = new List<MyObjStores>();

        //    foreach (var item in store)
        //    {
        //        MyObjStores dtnuevo = new MyObjStores();

        //        dtnuevo.id = item.CardCode.ToString();
        //        if (item.MailAddres == null)
        //        {

        //        }
        //        else {
        //            if (item.MailCity == null)
        //            {

        //            }
        //            else {
        //                if (item.MailZipCod == null)
        //                {


        //                }
        //                else {
        //                    dtnuevo.store = item.CardName.ToString() + ", " + item.MailAddres.ToString() + ", " + item.MailCity.ToString() + ", " + item.MailZipCod.ToString();

        //                    //GEOLOCALIZACION
        //                    try
        //                    {
        //                        string address = dtnuevo.store;
        //                        string requestUri = string.Format("https://maps.googleapis.com/maps/api/geocode/xml?key=AIzaSyC3zDvE8enJJUHLSmhFAdWhPRy_tNSdQ6g&address={0}&sensor=false", Uri.EscapeDataString(address));

        //                        WebRequest request = WebRequest.Create(requestUri);
        //                        WebResponse response = request.GetResponse();
        //                        XDocument xdoc = XDocument.Load(response.GetResponseStream());

        //                        XElement result = xdoc.Element("GeocodeResponse").Element("result");
        //                        XElement locationElement = result.Element("geometry").Element("location");
        //                        XElement lat = locationElement.Element("lat");
        //                        XElement lng = locationElement.Element("lng");
        //                        //NO SE PORQUE LO TIRA AL REVEZ
        //                        dtnuevo.lng = lng.Value;
        //                        dtnuevo.lat = lat.Value;
        //                        //FIN

        //                    }
        //                    catch
        //                    {
        //                        dtnuevo.lng = "";
        //                        dtnuevo.lat = "";
        //                    }
        //                    listaTiendas.Add(dtnuevo);
        //                }

        //            }
        //        }



        //    }



        //    ViewBag.lista = listaTiendas.ToList();
        //    return View();



        //}


        public ActionResult Cerrar_sesion()
        {
            Session.RemoveAll();
            return RedirectToAction("Index");
        }
    }


    
}