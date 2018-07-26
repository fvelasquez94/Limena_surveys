using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using Limena_surveys.Models;
using System.Web.Script.Serialization;

namespace Limena_surveys.Controllers
{
    public class HomeController : Controller
    {
        private DLI_SurveyEntities db = new DLI_SurveyEntities();
        private DLI_PROEntities DLIPRO = new DLI_PROEntities();



        public ActionResult Index()
        {


            return View();
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
            try
            {
                //var list = (from d in db.Vendedores select d).ToList();
                var obj = (from a in db.Vendedores where (a.correo == usuariocorreo && a.contrasena == password) select a).FirstOrDefault();
                if (obj != null)
                {


                    //Session["ID_vendedor"] = obj.ID_vendedor.ToString();
                    //Session["idSAP_Vendedor"] = obj.idSAP_vendedor.ToString();

                    int ID_customer = 0;
                    ID_customer = Convert.ToInt32(obj.idSAP_vendedor);

                    var clientes = (from b in DLIPRO.OCRDs where (b.SlpCode == ID_customer && b.CardType == "C") select b).OrderBy(b => b.CardName).ToList();



                    //IEnumerable <lsClientes> selectList_clientes = from st in clientes
                    //                                                  select new lsClientes
                    //                                                  {
                    //                                                      Value = Convert.ToString(st.CardCode),
                    //                                                      Text = st.CardCode.ToString() + " - " + st.CardName.ToString(),
                    //                                                      enabled = 3

                    //                                                };

                    //IEnumerable<lsClientes> selectList_clientes;

                    List<lsClientes> selectList_clientes = new List<lsClientes>();

                    foreach (var item in clientes) {
                        lsClientes nuevoc = new lsClientes();

                        nuevoc.Value = item.CardCode.ToString();
                        nuevoc.Text = item.CardCode.ToString() + " - " + item.CardName.ToString();

                        var existe = (from c in db.Detalle_encuesta where (c.idSAP_cliente == item.CardCode.ToString()) select c).ToList();

                        if (existe.Count > 0)
                        {
                            nuevoc.enabled = 0;
                        }
                        else {

                            nuevoc.enabled = 1;
                        }

                        selectList_clientes.Add(nuevoc);

                        existe = null;
                    }




                    JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
                    string result = javaScriptSerializer.Serialize(selectList_clientes);
                    return Json(new { success = true, lstclientes = result  }, JsonRequestBehavior.AllowGet);

                }
                else
                {
                    //Si ingreso mal la contraseña o el usuario no existe
                    return Json(new { success = false}, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false}, JsonRequestBehavior.AllowGet);


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


                if (values.Count > 0)
                {

                    foreach (var item in values)
                    {
                    Detalle_encuesta dtnuevo = new Detalle_encuesta();

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


                    return Json(new { Result = "Success" });
                }
                return Json(new { Result = "Warning" });


        }
        }
    
}