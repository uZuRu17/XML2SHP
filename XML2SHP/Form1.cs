using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using NetTopologySuite.IO.ShapeFile;
using NetTopologySuite.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite;
using NetTopologySuite.IO;

namespace XML2SHP
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //initial vars
            string futureFieldSector = "Sector";
            string futureFieldName = "ID";
            string futureIE = "IE";
            string futureFieldAreaCG = "Sup Mas";
            string futureFieldLegalCG = "Sup Acte";
            string futureFieldTitle = "Titlu";
            string futureFieldTarla = "Tarla";
            string futureFieldParcel = "Parcela";
            string futurePerson = "Persoana";
            string futureDead = "Decedat";
            string futureIntravilan = "Intra";
            string futureImprejmuit = "Ingradit";
            int nrCGXML = 0;
            int intr = 0;
            //Create a future list
            IList<Feature> futuresList = new List<Feature>();

            //browse
            string filePath = textBox1.Text;
            string[] filez = Directory.GetFiles(filePath, "*.cgxml", SearchOption.AllDirectories);
            var files = filez.Select(x => new FileInfo(x)).ToArray();

            for (int i = 0; i < (int)files.Length; i++)
            {
                nrCGXML++;
            }

            //loop trough cgxml
            for (int i = 0; i < (int)files.Length; i++)
            {
                FileInfo fo = files[i];
                CGXML fisier = new CGXML();
                try
                {
                    fisier.ReadXml(fo.FullName);
                }
                catch (Exception exception)
                {
                    Exception ex = exception;
                    MessageBox.Show(string.Concat(new string[] { "Eroare ", ex.GetType().ToString(), "\n", ex.Message, fo.FullName }));
                }

                //create geometry factory
                GeometryFactory geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
                Geometry[] gr = new Geometry[nrCGXML];
                foreach (CGXML.LandRow lr in fisier.Land)
                {
                    var r = 0;
                    string Person = "";
                    var q = 0;
                    int v = 0;
                    string intratest = "";
                    string ie = lr.E2IDENTIFIER;
                    string imprej = (lr.ENCLOSED ? "DA" : "NU").ToString();
                    int parcelCount = fisier.Parcel.Count;
                    string[] titleNO = new string[parcelCount];
                    string[] landLotNO = new string[parcelCount];
                    string[] parcelNO = new string[parcelCount];
                    List<bool> intraNO = new List<bool>();
                    string intraString = "";
                    Coordinate[] myCoord = new Coordinate[fisier.Points.Count + 1];
                    string[] personArr = new string[fisier.Person.Count];
                    string[] deadPersonArr = new string[fisier.Person.Count];
                    string sector = lr.CADSECTOR.ToString();
                    foreach (CGXML.PointsRow pr in fisier.Points)
                    {
                        if (pr.IMMOVABLEID != 9898989)
                        {
                            myCoord[r++] = new Coordinate(pr.X, pr.Y);
                        }
                    }
                    if (myCoord[r - 1] != myCoord[0])
                    {
                        myCoord[r] = myCoord[0];
                    }
                    foreach (CGXML.PersonRow pp in fisier.Person)
                    {
                        personArr[q] = string.Concat(pp.FIRSTNAME, " ", pp.LASTNAME);
                        deadPersonArr[q] = (pp.DEFUNCT ? "DA" : "NU");
                        q++;
                    }
                    foreach (CGXML.ParcelRow pr in fisier.Parcel)
                    {
                        titleNO[v] = pr.TITLENO;
                        landLotNO[v] = pr.LANDPLOTNO;
                        parcelNO[v] = pr.PARCELNO;
                        intraNO.Add(pr.INTRAVILAN);

                        v++;
                    }
                    string[] titleNO2 = titleNO.Distinct().ToArray();
                    titleNO2 = titleNO2.Where(f => f != null).ToArray();
                    string[] landLotNO2 = landLotNO.Distinct().ToArray();
                    landLotNO2 = landLotNO2.Where(p => p != null).ToArray();
                    string[] parcelNo2 = parcelNO.Distinct().ToArray();
                    parcelNo2 = parcelNO.Where(l => l != null).ToArray();
                    string titleNO3 = string.Join(" , ", titleNO2);
                    string landLotNO3 = string.Join(" , ", landLotNO2);
                    string parcelNO3 = string.Join(" , ", parcelNo2);
                    if (!intraNO.Contains(false) && intraNO.Contains(true))
                    {
                        intraString = "Intra";
                    }
                    else if (intraNO.Contains(false) && !intraNO.Contains(true))
                    {
                        intraString = "Extra";
                    }
                    else
                    {
                        intraString = "Mixt";
                    }
                    string personTest = string.Join(" , ", personArr);
                    string deadPesTest = string.Join(" , ", deadPersonArr);

                    //create the default table with fields - alternately use DBaseField classes
                    AttributesTable t = new AttributesTable();
                    t.Add(futureFieldSector, lr.CADSECTOR);
                    t.Add(futureFieldName, lr.CADGENNO);
                    t.Add(futureIE, lr.E2IDENTIFIER);
                    t.Add(futureFieldAreaCG, lr.MEASUREDAREA);
                    t.Add(futureFieldLegalCG, lr.PARCELLEGALAREA);
                    t.Add(futurePerson, personTest);
                    t.Add(futureDead, deadPesTest);
                    t.Add(futureFieldTitle, titleNO3);
                    t.Add(futureFieldTarla, landLotNO3);
                    t.Add(futureFieldParcel, parcelNO3);
                    t.Add(futureImprejmuit, imprej);
                    t.Add(futureIntravilan, intraString);
                    //Geometry 
                    myCoord = myCoord.Where(c => c != null).ToArray();
                    gr[intr] = geomFactory.CreatePolygon(myCoord);
                    futuresList.Add(new Feature(gr[intr], t));
                    intr++;
                }
            }
            //Feature list
            IList<Feature> features = futuresList.OfType<Feature>().ToList();
            string shapefile = string.Concat(filePath, "\\", "Imobile");
            ShapefileDataWriter writer = new ShapefileDataWriter(shapefile) { Header = ShapefileDataWriter.GetHeader(features[0], features.Count) };

            writer.Write(features);
        }
    }
}
