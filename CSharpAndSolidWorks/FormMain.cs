﻿using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;
using SolidWorks.Interop.swcommands;
using View = SolidWorks.Interop.sldworks.View;

namespace CSharpAndSolidWorks
{
    public partial class Btn_Filter : Form
    {
        public Btn_Filter()
        {
            InitializeComponent();
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            if (swApp != null)
            {
                string msg = "This message from C#. solidworks version is " + swApp.RevisionNumber();
                //发一个消息给solidworks用户
                swApp.SendMsgToUser(msg);
            }
        }

        private void BtnOpenAndNew_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            if (swApp != null)
            {
                //通过GetDocumentTemplate 获取默认模板的路径 ,第一个参数可以指定类型
                string partDefaultTemplate = swApp.GetDocumentTemplate((int)swDocumentTypes_e.swDocPART, "", 0, 0, 0);
                //也可以直接指定slddot asmdot drwdot
                //partDefaultTemplate = @"xxx\..prtdot";

                var newDoc = swApp.NewDocument(partDefaultTemplate, 0, 0, 0);

                if (newDoc != null)
                {
                    //创建完成
                    swApp.SendMsgToUser("Create done.");

                    //下面获取当前文件
                    ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

                    //选择对应的草图基准面
                    bool boolstatus = swModel.Extension.SelectByID2("Plane1", "PLANE", 0, 0, 0, false, 0, null, 0);

                    //创建一个2d草图
                    swModel.SketchManager.InsertSketch(true);

                    //画一条线 长度100mm  (solidworks 中系统单位是米,所以这里写0.1)
                    swModel.SketchManager.CreateLine(0, 0, 0, 0, 0.1, 0);

                    //关闭草图
                    swModel.SketchManager.InsertSketch(true);

                    //设定保存文件的完整路径
                    string myNewPartPath = @"C:\myNewPart.SLDPRT";

                    //保存零件.
                    int longstatus = swModel.SaveAs3(myNewPartPath, 0, 1);

                    //关闭零件
                    swApp.CloseDoc(myNewPartPath);
                    swApp.SendMsgToUser("Closed");
                    //重新打开零件.
                    swApp.OpenDoc(myNewPartPath, (int)swDocumentTypes_e.swDocPART);

                    swApp.SendMsgToUser("Open completed.");
                }
            }
        }

        private void BtnGetPartData_Click(object sender, EventArgs e)
        {
            //请先打开零件: ..\TemplateModel\clamp1.sldprt

            ISldWorks swApp = Utility.ConnectToSolidWorks();

            if (swApp != null)
            {
                ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc; //当前零件

                //获取通用属性值
                string project = swModel.GetCustomInfoValue("", "Project");

                swModel.DeleteCustomInfo2("", "Qty"); //删除指定项
                swModel.AddCustomInfo3("", "Qty", 30, "1"); //增加通用属性值

                var ConfigNames = (string[])swModel.GetConfigurationNames(); //所有配置名称

                Configuration swConfig = null;

                foreach (var configName in ConfigNames)//遍历所有配置
                {
                    swConfig = (Configuration)swModel.GetConfigurationByName(configName);

                    var manger = swModel.Extension.CustomPropertyManager[configName];
                    //删除当前配置中的属性
                    manger.Delete2("Code");
                    //增加一个属性到些配置
                    manger.Add3("Code", (int)swCustomInfoType_e.swCustomInfoText, "A-" + configName, (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue);
                    //获取此配置中的Code属性
                    string tempCode = manger.Get("Code");
                    //获取此配置中的Description属性

                    var tempDesc = manger.Get("Description");
                    Debug.Print("  Name of configuration  ---> " + configName + " Desc.=" + tempCode);
                }
            }
            else
            {
                MessageBox.Show("Please open a part first.");
            }
        }

        private void Btn_ChangeDim_Click(object sender, EventArgs e)
        {
            //请先打开零件: ..\TemplateModel\clamp1.sldprt
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            if (swApp != null)
            {
                //1.增加配置
                ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;
                string NewConfigName = "NewConfig";
                bool boolstatus = swModel.AddConfiguration2(NewConfigName, "", "", true, false, false, true, 256);

                swModel.ShowConfiguration2(NewConfigName);

                //2.增加特征(选择一条边，加圆角)
                boolstatus = swModel.Extension.SelectByID2("", "EDGE", 3.75842546947069E-03, 3.66350829162911E-02, 1.23295158888936E-03, false, 1, null, 0);

                Feature feature = swModel.FeatureManager.FeatureFillet3(195, 0.000508, 0.01, 0, 0, 0, 0, null, null, null, null, null, null, null);

                //3.压缩特征

                feature.Select(false);

                swModel.EditSuppress();

                //4.修改尺寸
                swModel.Parameter("D1@Fillet8").SystemValue = 0.000254; //0.001英寸

                swModel.EditRebuild3();

                //5.删除特征

                feature.Select(false);
                swModel.EditDelete();
            }
        }

        private void Btn_Traverse_Feature_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();
            //加速读取
            swApp.CommandInProgress = true;

            if (swApp != null)
            {
                ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

                //第一个特征
                Feature swFeat = (Feature)swModel.FirstFeature();

                //遍历
                Utility.TraverseFeatures(swFeat, true);
            }
            swApp.CommandInProgress = false;
        }

        private void Btn_Traverse_Comp_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            if (swApp != null)
            {
                ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

                Configuration swConf = swModel.GetActiveConfiguration();

                Component2 swRootComp = swConf.GetRootComponent();

                //遍历
                Utility.TraverseCompXform(swRootComp, 0);
            }
        }

        private void btn_Traverse_Drawing_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            if (swApp != null)
            {
                ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

                DrawingDoc drawingDoc = (DrawingDoc)swModel;

                //获取当前工程图中的所有图纸名称
                var sheetNames = drawingDoc.GetSheetNames();

                //遍历并找出包含k3 的工程图名称
                string k3Name = "";
                foreach (var kName in sheetNames)
                {
                    if (((String)kName).Contains("k3"))
                    {
                        k3Name = (String)kName;
                    }
                }
                //切换图纸
                bool bActSheet = drawingDoc.ActivateSheet(k3Name);

                // 获取当前工程图对象
                Sheet drwSheet = default(Sheet);
                drwSheet = (Sheet)drawingDoc.GetCurrentSheet();

                //获取所有的视图
                object[] views = null;
                views = (object[])drwSheet.GetViews();

                foreach (object vView in views)
                {
                    var ss = (View)vView;
                    Debug.Print(ss.GetName2());
                }

                //选中新的视图，移动位置。
                bool boolstatus = swModel.Extension.SelectByID2("主视图1", "DRAWINGVIEW", 0, 0, 0, false, 0, null, 0);
                //切换视图方向
                swModel.ShowNamedView2("*Front", (int)swStandardViews_e.swFrontView);
                //修改视图的名称
                swModel.SelectedFeatureProperties(0, 0, 0, 0, 0, 0, 0, true, false, "主视图-1");

                SelectionMgr modelSel = swModel.ISelectionManager;

                //该视图对象
                View actionView = (View)modelSel.GetSelectedObject5(1);

                //位置 actionView.Position

                //获取注释
                var noteCount = actionView.GetNoteCount();

                List<Note> AllNotes = new List<Note>();
                if (noteCount > 0)
                {
                    Note note = (Note)actionView.GetFirstNote();

                    Debug.Print(noteCount.ToString());
                    // note.GetBalloonStyle
                    Debug.Print(note.GetText());

                    AllNotes.Add(note);

                    var leaderInfo = note.GetLeaderInfo();

                    for (int k = 0; k < noteCount - 1; k++)
                    {
                        note = (Note)note.GetNext();
                        Debug.Print(note.GetText());

                        AllNotes.Add(note);
                    }

                    swModel.EditRebuild3();

                    swModel.EditDelete();
                }
            }
        }

        private void btn_InsertPart_Click(object sender, EventArgs e)
        {
            //step1:生成一个新装配并保存.
            ISldWorks swApp = Utility.ConnectToSolidWorks();
            int errors = 0;
            int warinings = 0;
            if (swApp != null)
            {
                //通过GetDocumentTemplate 获取默认模板的路径 ,第一个参数可以指定类型
                string partDefaultTemplate = swApp.GetDocumentTemplate((int)swDocumentTypes_e.swDocASSEMBLY, "", 0, 0, 0);
                //也可以直接指定slddot asmdot drwdot
                //partDefaultTemplate = @"xxx\..prtdot";

                var newDoc = swApp.NewDocument(partDefaultTemplate, 0, 0, 0);

                if (newDoc != null)
                {
                    //下面获取当前文件
                    ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

                    swModel.Extension.SaveAs(@"D:\09_Study\CSharpAndSolidWorks\CSharpAndSolidWorks\TemplateModel\TempAssembly.sldasm", 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, "", ref errors, ref warinings);

                    //step2:打开已有零件
                    string myNewPartPath = @"D:\09_Study\CSharpAndSolidWorks\CSharpAndSolidWorks\TemplateModel\clamp1.sldprt";
                    swApp.OpenDoc(myNewPartPath, (int)swDocumentTypes_e.swDocPART);

                    //step3:切换到装配体中,利用面配合来装配零件.

                    AssemblyDoc assemblyDoc = swApp.ActivateDoc3("TempAssembly.sldasm", true, 0, errors);
                    swApp.ActivateDoc("TempAssembly.sldasm");

                    Component2 InsertedComponent = assemblyDoc.AddComponent5(myNewPartPath, 0, "", false, "", 0, 0, 0);

                    InsertedComponent.Select(false);

                    assemblyDoc.UnfixComponent();

                    //step4: 配合:

                    bool boolstatus = swModel.Extension.SelectByID2("Plane1", "PLANE", 0, 0, 0, false, 0, null, 0);

                    boolstatus = swModel.Extension.SelectByID2("Front Plane@clamp1-1@TempAssembly", "PLANE", 0, 0, 0, true, 0, null, 0);
                    int longstatus = 0;
                    //重合
                    assemblyDoc.AddMate5(0, 0, false, 0, 0.001, 0.001, 0.001, 0.001, 0, 0, 0, false, false, 0, out longstatus);

                    swModel.EditRebuild3();
                    swModel.ClearSelection();

                    //距离配合 :
                    boolstatus = swModel.Extension.SelectByID2("Plane2", "PLANE", 0, 0, 0, false, 0, null, 0);
                    boolstatus = swModel.Extension.SelectByID2("Top Plane@clamp1-1@TempAssembly", "PLANE", 0, 0, 0, true, 0, null, 0);

                    assemblyDoc.AddMate5((int)swMateType_e.swMateDISTANCE, (int)swMateAlign_e.swMateAlignALIGNED, true, 0.01, 0.01, 0.01, 0.01, 0.01, 0, 0, 0, false, false, 0, out longstatus);
                }
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            if (swApp != null)
            {
                ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

                if (swModel.GetType() == (int)swDocumentTypes_e.swDocPART || swModel.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
                {
                    ModelDocExtension swModExt = (ModelDocExtension)swModel.Extension;

                    int error = 0;

                    int warnings = 0;

                    //设置导出版本
                    swApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swParasolidOutputVersion, (int)swParasolidOutputVersion_e.swParasolidOutputVersion_161);

                    swModExt.SaveAs(@"C:\export.x_t", (int)swSaveAsVersion_e.swSaveAsCurrentVersion, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref error, ref warnings);
                }
                else if (swModel.GetType() == (int)swDocumentTypes_e.swDocDRAWING)
                {
                    ModelDocExtension swModExt = (ModelDocExtension)swModel.Extension;

                    int error = 0;

                    int warnings = 0;

                    //设置dxf 导出版本 R14
                    swApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swDxfVersion, 2);

                    //是否显示 草图
                    swModel.SetUserPreferenceToggle(196, false);

                    swModExt.SaveAs(@"C:\export.dxf", (int)swSaveAsVersion_e.swSaveAsCurrentVersion, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref error, ref warnings);
                }
            }
        }

        public DispatchWrapper[] ObjectArrayToDispatchWrapperArray(object[] Objects)
        {
            int ArraySize = 0;
            ArraySize = Objects.GetUpperBound(0);
            DispatchWrapper[] d = new DispatchWrapper[ArraySize + 1];
            int ArrayIndex = 0;
            for (ArrayIndex = 0; ArrayIndex <= ArraySize; ArrayIndex++)
            {
                d[ArrayIndex] = new DispatchWrapper(Objects[ArrayIndex]);
            }
            return d;
        }

        public DispatchWrapper[] LibRefs;

        private void btnInsertLibF_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            //可以参考API帮助中的:Create Library Feature Data Object and Library Feature With References Example (C#)

            //Step1:新建一个零件.
            Feature swFeature = default(Feature);
            ModelDoc2 swModel = default(ModelDoc2);
            ModelDocExtension swModelDocExt = default(ModelDocExtension);
            SketchManager swSketchManager = default(SketchManager);
            SelectionMgr swSelectionManager = default(SelectionMgr);
            FeatureManager swFeatureManager = default(FeatureManager);
            LibraryFeatureData swLibFeat = default(LibraryFeatureData);
            bool status = false;
            object[] sketchLines = null;
            object Refs = null;
            object RefTypes = null;
            int RefCount = 0;
            int k = 0;
            int i = 0;
            DispatchWrapper[] LibRefs = null;

            string libPath = "C:\\ProgramData\\SOLIDWORKS\\SOLIDWORKS 2018\\design library\\features\\metric\\slots\\straight slot.sldlfp";

            // Create part
            swModel = (ModelDoc2)swApp.NewDocument("C:\\ProgramData\\SolidWorks\\SOLIDWORKS 2018\\templates\\Part.prtdot", 0, 0, 0);
            swModelDocExt = (ModelDocExtension)swModel.Extension;
            status = swModelDocExt.SelectByID2("Top Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
            swModel.ClearSelection2(true);
            status = swModelDocExt.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSketchAddConstToRectEntity, (int)swUserPreferenceOption_e.swDetailingNoOptionSpecified, false);
            status = swModelDocExt.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSketchAddConstLineDiagonalType, (int)swUserPreferenceOption_e.swDetailingNoOptionSpecified, true);
            swSketchManager = (SketchManager)swModel.SketchManager;
            sketchLines = (object[])swSketchManager.CreateCornerRectangle(0, 0, 0, 1, 0.5, 0);
            swModel.ShowNamedView2("*Trimetric", 8);
            swModel.ClearSelection2(true);
            status = swModelDocExt.SelectByID2("Line2", "SKETCHSEGMENT", 0, 0, 0, false, 0, null, 0);
            status = swModelDocExt.SelectByID2("Line1", "SKETCHSEGMENT", 0, 0, 0, true, 0, null, 0);
            status = swModelDocExt.SelectByID2("Line4", "SKETCHSEGMENT", 0, 0, 0, true, 0, null, 0);
            status = swModelDocExt.SelectByID2("Line3", "SKETCHSEGMENT", 0, 0, 0, true, 0, null, 0);
            swFeatureManager = (FeatureManager)swModel.FeatureManager;
            swFeature = (Feature)swFeatureManager.FeatureExtrusion2(true, false, false, 0, 0, 0.01, 0.01, false, false, false,
            false, 0.0174532925199433, 0.0174532925199433, false, false, false, false, true, true, true,
            0, 0, false);
            swSelectionManager = (SelectionMgr)swModel.SelectionManager;
            swSelectionManager.EnableContourSelection = false;

            swModel = (ModelDoc2)swApp.ActiveDoc;

            string actName = swModel.GetPathName();

            #region 第一种方法

            //Step2:初始化库特征
            swLibFeat = (LibraryFeatureData)swFeatureManager.CreateDefinition((int)swFeatureNameID_e.swFmLibraryFeature);
            status = swLibFeat.Initialize(libPath);

            //step3:获取库特征需要的参考对象
            RefCount = swLibFeat.GetReferencesCount();
            Refs = (object[])swLibFeat.GetReferences2((int)swLibFeatureData_e.swLibFeatureData_FeatureRespect, out RefTypes);

            if ((RefTypes != null))
            {
                Debug.Print("Types of references required (edge = 1): ");
                int[] RefType = (int[])RefTypes;
                for (k = RefType.GetLowerBound(0); k <= RefType.GetUpperBound(0); k++)
                {
                    Debug.Print("    " + RefType[k].ToString());
                }
            }
            //setp4:设定库特征默认的配置名称
            swLibFeat.ConfigurationName = "Default";
            //setp5:选择一个面,并插入库特征
            status = swModelDocExt.SelectByID2("", "FACE", 0.522458766456054, 0.288038964184011, 0.00999999999987722, false, 0, null, 0);
            swFeature = (Feature)swFeatureManager.CreateFeature(swLibFeat);
            //step6:
            swLibFeat = null;
            swLibFeat = (LibraryFeatureData)swFeature.GetDefinition();
            status = swLibFeat.AccessSelections(swModel, null);

            //step7:选择真实的参考
            status = swModelDocExt.SelectByID2("", "EDGE", 0.960865149149924, 0.497807163546383, 0.0131011390528215, true, 0, null, 0);
            status = swModelDocExt.SelectByID2("", "EDGE", 0.99866860703213, 0.481385806014544, 0.0113313929676906, true, 0, null, 0);
            int selCount = 0;
            selCount = swSelectionManager.GetSelectedObjectCount2(-1);

            object[] selectedObjects = new object[selCount];

            for (i = 0; i < selCount; i++)
            {
                object selectedObject = null;
                selectedObject = (object)swSelectionManager.GetSelectedObject6(i + 1, -1);
                selectedObjects[i] = selectedObject;
            }

            // 转换对象
            LibRefs = (DispatchWrapper[])ObjectArrayToDispatchWrapperArray((selectedObjects));

            // 设定引用关系到刚生成的库特征
            swLibFeat.SetReferences(LibRefs);

            // 更新库功能
            status = swFeature.ModifyDefinition(swLibFeat, swModel, null);

            // 取消抑制库功能
            status = swModelDocExt.SelectByID2("straight slot<1>", "BODYFEATURE", 0, 0, 0, false, 0, null, 0);
            swModel.EditUnsuppress2();

            swModel.ClearSelection2(true);

            #endregion 第一种方法

            #region 第二种方法(已过时)

            ////先选中线,再插入库特征.

            ////要先打开库特征,然后切换到当前零件,选中参考特征,最后插入特征库

            //int errors = 0;
            //int warnings = 0;

            //swApp.OpenDoc6(libPath, 1, 0, "", errors, warnings);

            //swModel = swApp.ActivateDoc2(actName, true, errors);

            //status = swModelDocExt.SelectByID2("", "FACE", 0.522458766456054, 0.288038964184011, 9.99999999987722E-03, false, 0, null, 0);
            //status = swModelDocExt.SelectByID2("", "EDGE", 0.960865149149924, 0.497807163546383, 0.0131011390528215, true, 1, null, 0);
            //status = swModelDocExt.SelectByID2("", "EDGE", 0.99866860703213, 0.481385806014544, 0.0113313929676906, true, 2, null, 0);

            //swModel.InsertLibraryFeature(libPath);

            #endregion 第二种方法(已过时)
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 modelDoc2 = swApp.ActiveDoc;
            //SelectionMgr selectionMgr = modelDoc2.SelectionManager;

            //设置可选择类型的数组
            swSelectType_e[] filters = new swSelectType_e[1];

            //让用户只能选择实体

            filters[0] = swSelectType_e.swSelSOLIDBODIES;

            swApp.SetSelectionFilters(filters, true);
        }

        private void btn_DeleteConstraints_Click(object sender, EventArgs e)
        {
            //请先打开clamp1这个零件

            ISldWorks swApp = Utility.ConnectToSolidWorks();
            ModelDoc2 swModel = swApp.ActiveDoc;
            SelectionMgr swSelMgr = swModel.SelectionManager;

            //选择草图
            swModel.Extension.SelectByID2("Sketch2", "SKETCH", 0, 0, 0, false, 4, null, 0);

            //进入编辑草图
            swModel.EditSketch();

            //获取当前草图对象
            Sketch swSketch = swModel.GetActiveSketch2();

            //获取该草图中的所有线
            object[] vSketchSeg = swSketch.GetSketchSegments();

            //定义选择
            SelectData swSelData = swSelMgr.CreateSelectData();

            SketchSegment swSketchSeg;
            //遍历线
            for (int i = 0; i < vSketchSeg.Length; i++)
            {
                swSketchSeg = (SketchSegment)vSketchSeg[i];

                swSketchSeg.Select4(false, swSelData);
                //删除关系
                swModel.SketchConstraintsDelAll();
            }

            object[] vSketchPt = (SketchPoint[])swSketch.GetSketchPoints2();
            SketchPoint swSketchPt;
            //遍历点
            for (int i = 0; i < vSketchPt.Length; i++)
            {
                swSketchPt = (SketchPoint)vSketchPt[i];
                swSketchPt.Select4(false, swSelData);
                swModel.SketchConstraintsDelAll();
            }
            //退出编辑草图
            swModel.InsertSketch2(true);

            swModel.ClearSelection2(true);
        }

        private void btnSelectNamedFace_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();
            ModelDoc2 swModel = swApp.ActiveDoc;
            SelectionMgr swSelMgr = swModel.SelectionManager;

            #region 零件中选择

            PartDoc part1 = (PartDoc)swModel;
            //在零件中选择
            Face2 face1 = part1.GetEntityByName("SFace1", (int)swSelectType_e.swSelFACES);
            Entity entity1 = (Entity)face1;
            entity1.Select(false);

            #endregion 零件中选择

            #region 装配中选择

            //这里我们默认该零件已经是选中装配,否则我们需要遍历一次零件,仅做示例

            Component2 component = swSelMgr.GetSelectedObjectsComponent4(1, -1);

            swModel.ClearSelection();

            ModelDoc2 modelDoc = component.GetModelDoc2();

            //转换为PartDoc
            PartDoc part = (PartDoc)modelDoc;

            Face2 face = part.GetEntityByName("SFace1", (int)swSelectType_e.swSelFACES);

            Entity entity = (Entity)face;

            //在装配中再转换成装配中的实体
            Entity entityInComp = (Entity)component.GetCorrespondingEntity(entity);

            entityInComp.Select(false);

            #endregion 装配中选择
        }

        private void Btn_T_sketchsegment_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = default(ModelDoc2);
            ModelDocExtension swModelDocExt = default(ModelDocExtension);
            SelectionMgr swSelMgr = default(SelectionMgr);
            Feature swFeature = default(Feature);
            string fileName = null;
            bool status = false;
            int errors = 0;
            int warnings = 0;

            //打开文件
            fileName = "C:\\Users\\Public\\Documents\\SOLIDWORKS\\SOLIDWORKS 2018\\samples\\tutorial\\tolanalyst\\offset\\top_plate.sldprt";
            swModel = (ModelDoc2)swApp.OpenDoc6(fileName, (int)swDocumentTypes_e.swDocPART, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref errors, ref warnings);

            swModelDocExt = (ModelDocExtension)swModel.Extension;

            //选中草图
            status = swModelDocExt.SelectByID2("Sketch1", "SKETCH", 0, 0, 0, false, 0, null, 0);

            swSelMgr = (SelectionMgr)swModel.SelectionManager;
            //转换
            swFeature = (Feature)swSelMgr.GetSelectedObject6(1, -1);
            //进入编辑草图
            swModel.EditSketch();
            //获取草图中的所有线
            object[] vSketchSeg = (object[])swFeature.GetSpecificFeature2().GetSketchSegments();

            SketchSegment swSketchSeg;
            double totalLenth = 0;
            foreach (var tempSeg in vSketchSeg)
            {
                swSketchSeg = (SketchSegment)tempSeg;
                //这里判断 不是文本,并且不是中心线 则加入长度
                if (swSketchSeg.GetType() != (int)swSketchSegments_e.swSketchTEXT && swSketchSeg.ConstructionGeometry == false)
                {
                    totalLenth = totalLenth + swSketchSeg.GetLength();
                }
            }

            swModel.EditSketch();
            //显示总长
            swApp.SendMsgToUser("Total Length:" + totalLenth * 1000);
        }

        private ModelDoc2 m_RefDoc; //增加第三方数据流 共用模型.

        private void btn_ThridData_Click(object sender, EventArgs e)
        {
            //https://www.codestack.net/solidworks-api/data-storage/third-party/embed-file/
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = default(ModelDoc2);
            ModelDocExtension swModelDocExt = default(ModelDocExtension);

            swModel = swApp.ActiveDoc;
            m_RefDoc = swModel;

            switch (swModel.GetType())
            {
                case (int)swDocumentTypes_e.swDocPART:
                    (swModel as PartDoc).SaveToStorageNotify += new DPartDocEvents_SaveToStorageNotifyEventHandler(OnSaveToStorage);
                    break;

                case (int)swDocumentTypes_e.swDocASSEMBLY:
                    (swModel as AssemblyDoc).SaveToStorageNotify += new DAssemblyDocEvents_SaveToStorageNotifyEventHandler(OnSaveToStorage);
                    break;
            }

            swModel.SetSaveFlag();
            swApp.SendMsgToUser("请手动保存文件!这样会把数据流写入文件中.");
        }

        private int OnSaveToStorage()
        {
            IStream iStr = (IStream)m_RefDoc.IGet3rdPartyStorage("Tool.Name", true);

            using (ComStream comStr = new ComStream(iStr))
            {
                byte[] data = Encoding.Unicode.GetBytes("Paine's Tool");
                comStr.Write(data, 0, data.Length);
            }

            m_RefDoc.IRelease3rdPartyStorage("Tool.Name");

            return 0;
        }

        private void btn_LoadThrid_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            // ModelDoc2 swModel = default(ModelDoc2);

            IModelDoc2 doc = swApp.IActiveDoc2;
            ISelectionMgr selMgr = doc.ISelectionManager;
            //  IComponent2 comp = selMgr.GetSelectedObjectsComponent3(1, -1);

            IStream iStr = (IStream)doc.IGet3rdPartyStorage("Tool.Name", false);

            if (iStr != null)
            {
                using (ComStream comStr = new ComStream(iStr))
                {
                    byte[] data = new byte[comStr.Length];
                    comStr.Read(data, 0, (int)comStr.Length);

                    string strData = Encoding.Unicode.GetString(data);
                    MessageBox.Show(strData);
                }

                doc.IRelease3rdPartyStorage("Tool.Name");
            }
        }

        private void btn_Tips_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = default(ModelDoc2);
            ModelDocExtension swModelDocExt = default(ModelDocExtension);

            Frame swFrame = swApp.Frame();

            swFrame.SetStatusBarText("这里是提示信息-->");

            swApp.SendMsgToUser("下面提示显示进度条:");

            UserProgressBar userProgressBar;

            swApp.GetUserProgressBar(out userProgressBar);

            userProgressBar.Start(0, 100, "Status");

            int Position = 0;

            for (int i = 0; i <= 100; i++)
            {
                Position = i * 10;

                if (Position == 100)
                {
                    Position = 0;
                    break;
                }

                var lRet = userProgressBar.UpdateProgress(Position);
                userProgressBar.UpdateTitle("当前进度--->" + Position);

                swApp.SendMsgToUser("当前进度--->" + Position);
            }

            userProgressBar.End();
        }

        #region 高级选择

        private void btn_Adv_Select_Click(object sender, EventArgs e)
        {
            //请先打开C:\Users\Public\Documents\SOLIDWORKS\SOLIDWORKS 2018\samples\tutorial\api\landing_gear.sldasm

            //参考资料为API 帮助中的 Use Advanced Component Selection Example (C#)

            ISldWorks swApp = Utility.ConnectToSolidWorks();
            ModelDoc2 swModel = default(ModelDoc2);

            swModel = swApp.ActiveDoc;

            int DocType = 0;
            DocType = swModel.GetType();

            if (DocType != (int)swDocumentTypes_e.swDocASSEMBLY)
            {
                swApp.SendMsgToUser("当前不是装配体!");
                return;
            }

            AdvancedSelectionCriteria advancedSelectionCriteria = default(AdvancedSelectionCriteria);

            AssemblyDoc assemblyDoc = (AssemblyDoc)swModel;

            advancedSelectionCriteria = assemblyDoc.GetAdvancedSelection();

            int count = advancedSelectionCriteria.GetItemCount();

            //清空选择条件
            for (int i = 0; i < advancedSelectionCriteria.GetItemCount(); i++)
            {
                advancedSelectionCriteria.DeleteItem(i);
            }

            //增加选择条件 : 文件名包含lnk   具体的其它组合条件请看API帮助
            advancedSelectionCriteria.AddItem("Document name -- SW Special", 16, "lnk", false);
            //增加选择条件(或者) : 文件名包含hub
            advancedSelectionCriteria.AddItem("Document name -- SW Special", 16, "hub", false);

            //解释当前的选择条件
            ReportAllValues(advancedSelectionCriteria);

            //选择
            var SelectSuccess = advancedSelectionCriteria.Select();

            if (SelectSuccess == true)//选择成功
            {
                SelectionMgr selectionMgr = swModel.SelectionManager;
                Component2 swComp;
                //遍历已经选择的零件
                for (int j = 0; j < selectionMgr.GetSelectedObjectCount(); j++)
                {
                    swComp = selectionMgr.GetSelectedObject6(j + 1, 0);

                    swModel = swComp.GetModelDoc2();

                    //显示文件名
                    Debug.Print(swModel.GetPathName());
                }
            }
        }

        public string GetStringFromEnum(int EnumVal)
        {
            string functionReturnValue = null;
            //From enum swAdvSelecType_e
            if (EnumVal == 1)
            {
                functionReturnValue = "And";
            }
            else if (EnumVal == 2)
            {
                functionReturnValue = "Or";
            }
            else if (EnumVal == 16384)
            {
                functionReturnValue = "is yes";
            }
            else if (EnumVal == 32768)
            {
                functionReturnValue = "is no";
            }
            else if (EnumVal == 8)
            {
                functionReturnValue = "is not";
            }
            else if (EnumVal == 16)
            {
                functionReturnValue = "contains";
            }
            else if (EnumVal == 32)
            {
                functionReturnValue = "Is_Contained_By";
            }
            else if (EnumVal == 64)
            {
                functionReturnValue = "Interferes_With";
            }
            else if (EnumVal == 128)
            {
                functionReturnValue = "Does_Not_Interferes_With";
            }
            else if (EnumVal == 4)
            {
                functionReturnValue = "is (exactly)";
            }
            else if (EnumVal == 8192)
            {
                functionReturnValue = "not =";
            }
            else if (EnumVal == 512)
            {
                functionReturnValue = "<";
            }
            else if (EnumVal == 2048)
            {
                functionReturnValue = "<=";
            }
            else if (EnumVal == 4096)
            {
                functionReturnValue = "=";
            }
            else if (EnumVal == 1024)
            {
                functionReturnValue = ">=";
            }
            else if (EnumVal == 256)
            {
                functionReturnValue = ">";
            }
            else
            {
                functionReturnValue = "Condition NOT found";
            }
            return functionReturnValue;
        }

        public void ReportAllValues(AdvancedSelectionCriteria AdvancedSelectionCriteria)
        {
            Debug.Print("");

            int Count = 0;
            Count = AdvancedSelectionCriteria.GetItemCount();
            Debug.Print("GetItemCount returned " + Count);

            int i = 0;
            string aProperty = "";
            int Condition = 0;
            string Value = "";
            bool IsAnd = false;
            int Rindex = 0;
            string ConditionString = null;
            string PrintString = null;

            string IndexFmt = null;
            string RindexFmt = null;
            string AndOrFmt = null;
            string PropertyFmt = null;
            string ConditionFmt = null;
            string ValueFmt = null;
            IndexFmt = "!@@@@@@@@";
            RindexFmt = "!@@@@@@@@@";
            AndOrFmt = "!@@@@@@@@@";
            PropertyFmt = "!@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@";
            ConditionFmt = "!@@@@@@@@@@@@@@@";
            ValueFmt = "#.00";

            //Debug.Print
            PrintString = string.Format("Index", IndexFmt) + "     " + string.Format("Rindex", RindexFmt) + "  " + string.Format("And/Or", AndOrFmt) + "  " + string.Format("Property", PropertyFmt) + "                     " + string.Format("Condition", ConditionFmt) + "     " + string.Format("Value", ValueFmt);
            Debug.Print(PrintString);
            for (i = 0; i <= Count - 1; i++)
            {
                Rindex = AdvancedSelectionCriteria.GetItem(i, out aProperty, out Condition, out Value, out IsAnd);
                ConditionString = GetStringFromEnum(Condition);
                PrintString = string.Format(i.ToString(), IndexFmt) + "         " + string.Format(Rindex.ToString(), RindexFmt) + "       " + string.Format((IsAnd == false ? "OR" : "AND"), AndOrFmt) + "      " + string.Format(aProperty, PropertyFmt) + "  " + string.Format(ConditionString, ConditionFmt) + "  " + string.Format(Value, ValueFmt);
                Debug.Print(PrintString);
            }
            Debug.Print("");
        }

        #endregion 高级选择

        private void btnBounding_Click(object sender, EventArgs e)
        {
            //首先请打开一个零件.

            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = swApp.ActiveDoc;

            FeatureManager featureManager = swModel.FeatureManager;

            PartDoc partDoc = (PartDoc)swModel;
            //通过特征名字获取特征
            Feature feature = partDoc.FeatureByName("Bounding Box");
            int longstatus;
            if (feature == null)//特征为null时将创建Bounding Box

            {
                feature = featureManager.InsertGlobalBoundingBox((int)swGlobalBoundingBoxFitOptions_e.swBoundingBoxType_BestFit, true, false, out longstatus);
            }

            // 显示 Bounding Box sketch
            var b = swModel.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swViewDispGlobalBBox, true);

            //获取自动生成的属性值
            string str;
            string str2;
            string str3;
            string str4;
            IConfiguration configuration = swModel.GetActiveConfiguration();
            CustomPropertyManager manager2 = swModel.Extension.get_CustomPropertyManager(configuration.Name);

            manager2.Get3("Total Bounding Box Length", true, out str, out str2);
            manager2.Get3("Total Bounding Box Width", true, out str, out str3);
            manager2.Get3("Total Bounding Box Thickness", true, out str, out str4);

            swApp.SendMsgToUser($"size={str2}x{str3}x{str4}");
        }

        private void btn_Measure_Click(object sender, EventArgs e)
        {
            //请先打开../TemplateModel/Measure.SLDPRT  并选中保存的选择--SelMeasure
            //
            //返回指定草图中所有线的总长 请参考之前的遍历草图对象

            //下面的代码是获取零件的体积.
            //可以参考API帮助 的实例 Measure Selected Entities Example (C#)

            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = swApp.ActiveDoc;

            ModelDocExtension swModelDocExt = (ModelDocExtension)swModel.Extension;

            Measure swMeasure = (Measure)swModelDocExt.CreateMeasure();

            swMeasure.ArcOption = 0;

            bool status = swMeasure.Calculate(null);

            if (status)
            {
                swApp.SendMsgToUser((swMeasure.Distance * 1000).ToString());
            }
        }

        private void btn_GetMass_Click(object sender, EventArgs e)
        {
            // 获取质量属性可参考 Get Mass Properties of Visible and Hidden Components Example (C#)

            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = swApp.ActiveDoc;

            ModelDocExtension swModelDocExt = (ModelDocExtension)swModel.Extension;

            swModelDocExt.IncludeMassPropertiesOfHiddenBodies = false;
            int massStatus = 0;

            double[] massProperties = (double[])swModelDocExt.GetMassProperties(1, ref massStatus);
            if ((massProperties != null))
            {
                Debug.Print(" CenterOfMassX = " + massProperties[0]);
                Debug.Print(" CenterOfMassY = " + massProperties[1]);
                Debug.Print(" CenterOfMassZ = " + massProperties[2]);
                Debug.Print(" Volume = " + massProperties[3]);
                Debug.Print(" Area = " + massProperties[4]);
                Debug.Print(" Mass = " + massProperties[5]);
                Debug.Print(" MomXX = " + massProperties[6]);
                Debug.Print(" MomYY = " + massProperties[7]);
                Debug.Print(" MomZZ = " + massProperties[8]);
                Debug.Print(" MomXY = " + massProperties[9]);
                Debug.Print(" MomZX = " + massProperties[10]);
                Debug.Print(" MomYZ = " + massProperties[11]);
            }
            Debug.Print("-------------------------------");
        }

        private TaskpaneView taskpaneView;

        private void btn_Pane_Click(object sender, EventArgs e)
        {
            //注意: 这里只是显示自己的窗体到solidworks中,目前还是走的exe的方式 .
            //真正开发的时候应该在DLL中加入,这样速度会快很多.  exe读bom需要40s dll 只需要3秒左右.
            //获取当前程序所在路径
            string Dllpath = Path.GetDirectoryName(typeof(MyPane).Assembly.CodeBase).Replace(@"file:\", string.Empty);

            var imagePath = Path.Combine(Dllpath, "bomlist.bmp");

            ISldWorks swApp = Utility.ConnectToSolidWorks();

            string toolTip;

            toolTip = "BOM List";

            //创建页面
            if (taskpaneView != null)
            {
                taskpaneView.DeleteView();
                Marshal.FinalReleaseComObject(taskpaneView);
                taskpaneView = null;
            }

            taskpaneView = swApp.CreateTaskpaneView2(imagePath, toolTip);

            MyPane myPane = new MyPane(swApp);

            myPane.Dock = DockStyle.Fill;
            // myPane.Show();

            //在页面中显示窗体(嵌入)

            taskpaneView.DisplayWindowFromHandlex64(myPane.Handle.ToInt64());
        }

        private void Btn_Filter_Load(object sender, EventArgs e)
        {
        }

        private void Btn_Filter_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                //删除新加的控件
                // taskpaneView = null;
                taskpaneView.DeleteView();
                Marshal.FinalReleaseComObject(taskpaneView);
                taskpaneView = null;
            }
            catch (Exception exception)
            {
            }
        }

        private void btn_SetMaterial_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = swApp.ActiveDoc;
            ModelDocExtension swModelDocExt = (ModelDocExtension)swModel.Extension;
            string swMateDB = "";
            string tempMaterial = "";
            //获取现有材质
            tempMaterial = ((PartDoc)swModel).GetMaterialPropertyName2("", out swMateDB);

            MessageBox.Show($"当前零件材质为 {swMateDB} 中的 {tempMaterial} ");

            string configName = null;
            string databaseName = null;
            string newPropName = null;
            configName = "默认";
            databaseName = "SOLIDWORKS Materials";
            newPropName = "Beech";
            ((PartDoc)swModel).SetMaterialPropertyName2(configName, databaseName, newPropName);

            tempMaterial = ((PartDoc)swModel).GetMaterialPropertyName2("", out swMateDB);

            MessageBox.Show($"修改之后  当前零件材质为 {swMateDB} 中的 {tempMaterial} ");
        }

        private void btnSetColor_Click(object sender, EventArgs e)
        {
            //首先选择一个面.  点击按钮,将修改为红色.

            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = swApp.ActiveDoc;

            SelectionMgr selectionMgr = swModel.SelectionManager;
            try
            {
                for (int i = 1; i <= selectionMgr.GetSelectedObjectCount(); i++)
                {
                    Face2 face2 = (Face2)selectionMgr.GetSelectedObject6(i, -1);
                    var vFaceProp = swModel.MaterialPropertyValues;

                    var vProps = face2.GetMaterialPropertyValues2(1, null);
                    vProps[0] = 1;
                    vProps[1] = 0;
                    vProps[2] = 0;
                    vProps[3] = vFaceProp[3];
                    vProps[4] = vFaceProp[4];
                    vProps[5] = vFaceProp[5];
                    vProps[6] = vFaceProp[6];
                    vProps[7] = vFaceProp[7];
                    vProps[8] = vFaceProp[8];

                    face2.SetMaterialPropertyValues2(vProps, 1, null);
                    vProps = null;

                    vFaceProp = null;
                }

                swModel.ClearSelection2(true);
            }
            catch (Exception)
            {
                MessageBox.Show("请选择面,其它类型无效!");
            }
        }

        private void Btn_ReplacePart_Click(object sender, EventArgs e)
        {
            //首先打开 TempAssembly.sldasm
            //运行后,程序会把装配体中的Clamp1零件替换成Clamp2

            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = swApp.ActiveDoc;

            ModelDocExtension swModelDocExt = (ModelDocExtension)swModel.Extension;

            SelectionMgr selectionMgr = swModel.SelectionManager;

            AssemblyDoc assemblyDoc = (AssemblyDoc)swModel;

            //替换为同目录下的clamp2
            string ReplacePartPath = Path.GetDirectoryName(swModel.GetPathName()) + @"\clamp2.sldprt";

            bool boolstatus;

            //选择当前的clamp1
            boolstatus = swModelDocExt.SelectByID2("clamp1-1@TempAssembly", "COMPONENT", 0, 0, 0, false, 0, null, 0);

            boolstatus = assemblyDoc.ReplaceComponents2(ReplacePartPath, "", false, 0, true);

            if (boolstatus == true)
            {
                MessageBox.Show("替换完成!");
            }
        }

        private void btn_Add_CenterPoint_Click(object sender, EventArgs e)
        {
            //Open CenterPoint.SLDPRT

            AddCenterPointForSketch addCenterPointForSketch = new AddCenterPointForSketch();

            addCenterPointForSketch.CreateHeaterCenter("CenterLine");

            MessageBox.Show("中心点创建完成!");
        }

        private void btnInsertNote_Click(object sender, EventArgs e)
        {
            FrmNote frmNote = new FrmNote();

            frmNote.Show();
        }

        private void btnPackFile_Click(object sender, EventArgs e)
        {
            FrmCopy frmCopy = new FrmCopy(@"C:\TempAssembly.sldasm", @"D:\CopyTest.sldasm");

            frmCopy.Show();
        }

        private void btn_SelectByRay_Click(object sender, EventArgs e)
        {
            //连接到Solidworks
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

            Face2 swSelFace = default(Face2);
            SelectionMgr swSelMgr = (SelectionMgr)swModel.SelectionManager;

            //获取选择数据
            SelectData swSelData = default(SelectData);

            swSelData = swSelMgr.CreateSelectData();

            swSelFace = (Face2)swSelMgr.GetSelectedObject6(1, 0);

            var t = (double[])swSelFace.Normal;

            //获取屏幕鼠标选择的那个点
            var mousePoint = (double[])swSelMgr.GetSelectionPoint2(1, 0);

            swModel.ClearSelection2(true);

            //创建Ray选择

            var boolstatus = swModel.Extension.SelectByRay(mousePoint[0], mousePoint[1], mousePoint[2], t[0], t[1], t[2], 0.1, 2, false, 0, 0);

            if (boolstatus == true)
            {
                MessageBox.Show("选择完成!");
            }
        }

        private void GetDrawingModel_Click(object sender, EventArgs e)
        {
            //连接到Solidworks
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

            // DrawingDoc dc = (DrawingDoc)swModel;

            SelectionMgr selectionMgr = (SelectionMgr)swModel.SelectionManager;

            //获取选择的视图对象
            View view = (View)selectionMgr.GetSelectedObject5(1);

            //获取视图中的引用模型
            var viewModel = view.ReferencedDocument;

            //其它读取属性请参考博文 读取零件属性 ->BtnGetPartData_Click

            MessageBox.Show(viewModel.GetPathName());
        }

        private void btn_Part_Export_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ExportForm exportForm = new ExportForm(swApp);

            exportForm.Show();
        }

        private void btn_Scale_Click(object sender, EventArgs e)
        {
            FrmScreen frmScreen = new FrmScreen();

            frmScreen.Show();
        }

        private void btn_Transform_PartToAsm_Click(object sender, EventArgs e)
        {
            //连接到Solidworks

            //这个例子是把零件中的一个基准轴 的两个点的坐标转换到装配体中

            //请打开装配体，并在某个零件下选择一下基准轴

            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

            SelectionMgr swSelMgr = swModel.ISelectionManager;

            Feature swFeat = (Feature)swSelMgr.GetSelectedObject6(1, 0);

            String sAxisName = swFeat.Name;

            RefAxis RefAxis = swFeat.GetSpecificFeature2();

            var vParam = RefAxis.GetRefAxisParams();

            Component2 inletPart = swSelMgr.GetSelectedObjectsComponent4(1, 0);

            double[] nPt = new double[3];
            double[] nPt2 = new double[3];

            object vPt;
            object vPt2;

            nPt[0] = vParam[0]; nPt[1] = vParam[1]; nPt[2] = vParam[2];
            nPt2[0] = vParam[3]; nPt2[1] = vParam[4]; nPt2[2] = vParam[5];

            vPt = nPt;
            vPt2 = nPt2;

            MathUtility swMathUtil = (MathUtility)swApp.GetMathUtility();

            MathTransform mathTransform = inletPart.Transform2;

            MathTransform swXform = (MathTransform)mathTransform;

            MathPoint swMathPt = (MathPoint)swMathUtil.CreatePoint((vPt));

            MathPoint swMathPt2 = (MathPoint)swMathUtil.CreatePoint((vPt2));

            //swXform.Inverse(); 反转的话就是把装配体中的点坐标转到零件对应的坐标系统中

            swMathPt = (MathPoint)swMathPt.MultiplyTransform(swXform);

            swMathPt2 = (MathPoint)swMathPt2.MultiplyTransform(swXform);

            var x = swMathPt.ArrayData[0];
            var y = swMathPt.ArrayData[1];
            var z = swMathPt.ArrayData[2];
            var x2 = swMathPt2.ArrayData[0];
            var y2 = swMathPt2.ArrayData[1];
            var z2 = swMathPt2.ArrayData[2];

            var v1 = x2 - x;
            var v2 = y2 - y;
            var v3 = z2 - z;

            if (Math.Round(v3, 4) != 0 && Math.Round(v1, 4) == 0 && Math.Round(v2, 4) == 0)
            {
                MessageBox.Show("此轴在Z方向上");
            }

            //  MathVector mathVector = new MathVector();
        }

        private void btn_Insert_Block_Click(object sender, EventArgs e)
        {
            //连接到Solidworks
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

            DrawingDoc dc = (DrawingDoc)swModel;

            // SelectionMgr selectionMgr = (SelectionMgr)swModel.SelectionManager;

            double[] nPt = new double[3];

            nPt[0] = 0;
            nPt[1] = 0;
            nPt[2] = 0;

            MathUtility swMathUtil = swApp.GetMathUtility();

            MathPoint swMathPoint = swMathUtil.CreatePoint(nPt);

            double blockScale = 1;

            string blockPath = @"D:\09_Study\CSharpAndSolidWorks\CSharpAndSolidWorks\TemplateModel\TestBlock.SLDBLK";

            //插入图块
            var swBlockInst = InsertBlockReturnInst(swApp, swModel, swMathPoint, blockPath, blockScale);

            //  swModel.SketchManager.MakeSketchBlockFromFile(mathPoint, blockPath, false, blockScale, 0);

            //修改块的属性(如果只是普通的图块，则无需要这一步。直接使用上面的一行插入图块即可)
            swBlockInst.SetAttributeValue("Title1", "Paine");
        }

        /// <summary>
        /// 插入并返回最后一个图块实例
        /// </summary>
        /// <param name="sldWorks"></param>
        /// <param name="modelDoc2"></param>
        /// <param name="mathPoint"></param>
        /// <param name="blockPath"></param>
        /// <param name="blockScale"></param>
        /// <returns></returns>
        private SketchBlockInstance InsertBlockReturnInst(ISldWorks sldWorks, ModelDoc2 modelDoc2, MathPoint mathPoint, String blockPath, double blockScale)
        {
            SketchBlockInstance swBlockInst;
            List<String> NowBlockName = new List<String>();
            var swModel = modelDoc2;
            Boolean boolstatus = swModel.Extension.SelectByID2(System.IO.Path.GetFileNameWithoutExtension(blockPath), "SUBSKETCHDEF", 0, 0, 0, false, 0, null, 0);

            if (boolstatus == true)
            {
                Feature swFeat = swModel.SelectionManager.GetSelectedObject6(1, 0);
                var swSketchBlockDef = swFeat.GetSpecificFeature2();

                var nbrBlockInst = swSketchBlockDef.GetInstanceCount;

                if (nbrBlockInst > 0)
                {
                    var vBlockInst = swSketchBlockDef.GetInstances();

                    for (int i = 0; i < nbrBlockInst; i++)
                    {
                        swBlockInst = vBlockInst[i];

                        NowBlockName.Add(swBlockInst.Name.ToString());
                    }

                    swModel.SketchManager.MakeSketchBlockFromFile(mathPoint, blockPath, false, blockScale, 0);

                    swModel.ClearSelection2(true);

                    boolstatus = swModel.Extension.SelectByID2(System.IO.Path.GetFileNameWithoutExtension(blockPath), "SUBSKETCHDEF", 0, 0, 0, false, 0, null, 0);

                    swFeat = swModel.SelectionManager.GetSelectedObject6(1, 0);
                    swSketchBlockDef = swFeat.GetSpecificFeature2();

                    nbrBlockInst = swSketchBlockDef.GetInstanceCount;

                    if (nbrBlockInst > 0)
                    {
                        vBlockInst = swSketchBlockDef.GetInstances();

                        for (int j = 0; j < nbrBlockInst; j++)
                        {
                            swBlockInst = vBlockInst[j];
                            if (!NowBlockName.Contains(swBlockInst.Name.ToString()))
                            {
                                swModel.Extension.SelectByID2(swBlockInst.Name, "SUBSKETCHINST", 0, 0, 0, false, 0, null, 0);
                                swBlockInst = GetSketchBlockInstanceFromSelection();
                                return swBlockInst;
                            }
                        }
                    }
                    return null;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var swSketchBlockDef = swModel.SketchManager.MakeSketchBlockFromFile(mathPoint, blockPath, false, blockScale, 0);
                swBlockInst = swSketchBlockDef.GetInstances()[0];

                return swBlockInst;
            }
        }

        private SketchBlockInstance GetSketchBlockInstanceFromSelection()
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel;
            ModelDocExtension swModelDocExt;
            SketchBlockInstance SketchBlockInstance;

            DateTime time = DateTime.Now;

            try
            {
                swModel = swApp.ActiveDoc;
                swModelDocExt = swModel.Extension;

                SelectionMgr swSelectionMgr;
                swSelectionMgr = swModel.SelectionManager;

                string SelectByString = "";
                string ObjectType = "";
                int type;
                double x;
                double y;
                double z;

                if (swSelectionMgr.GetSelectedObjectCount2(-1) > 1)
                {
                    // Return only a SketchblockInstance when only one is selected...
                    // modify if you want return more than one (or only the first) selected Sketchblockinstance
                    return null;
                }

                swSelectionMgr.GetSelectionSpecification(1, out SelectByString, out ObjectType, out type, out x, out y, out z);
                Debug.WriteLine(SelectByString + " " + ObjectType + " " + type);

                if (type == (int)swSelectType_e.swSelSUBSKETCHINST)
                {
                    SketchBlockInstance = swSelectionMgr.GetSelectedObject6(1, -1);
                    Debug.WriteLine("Found:" + SketchBlockInstance.Name);
                    return SketchBlockInstance;
                }
                else if (type == (int)swSelectType_e.swSelSKETCHSEGS | type == (int)swSelectType_e.swSelSKETCHPOINTS)
                {
                    // Show if a sketchblockinstance has the same name
                    SketchManager SwSketchMgr;
                    SwSketchMgr = swModel.SketchManager;

                    object[] blockDefinitions = (object[])SwSketchMgr.GetSketchBlockDefinitions();
                    foreach (SketchBlockDefinition blockDef in blockDefinitions)
                    {
                        foreach (SketchBlockInstance blockInstance in blockDef.GetInstances())
                        {
                            if (SelectByString.EndsWith(blockInstance.Name))
                            {
                                Debug.WriteLine("Found:" + blockInstance.Name);
                                return blockInstance;
                            }
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                Debug.WriteLine(DateTime.Now.Subtract(time).Milliseconds);
            }

            return null;
        }

        private void btn_setcolor_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            if (swApp != null)
            {
                ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

                Configuration swConf = swModel.GetActiveConfiguration();

                Component2 swRootComp = swConf.GetRootComponent();

                //遍历

                Utility.TraverseCompXform(swRootComp, 0, true);

                swModel.WindowRedraw();

                swModel.EditRebuild3();
            }
        }

        private void butGlobalVariables_Click(object sender, EventArgs e)
        {
            //连接solidworks
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            if (swApp != null)
            {
                //获取当前模型
                ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;
                //定义方程式管理器
                EquationMgr swEqnMgr = default(EquationMgr);

                int i = 0;
                int nCount = 0;

                if (swModel != null)
                {
                    swEqnMgr = (EquationMgr)swModel.GetEquationMgr();
                    // nCount = swEqnMgr.GetCount();
                    //for (i = 0; i < nCount; i++)
                    //{
                    //    Debug.Print("  Equation(" + i + ")  = " + swEqnMgr.get_Equation(i));
                    //    Debug.Print("    Value = " + swEqnMgr.get_Value(i));
                    //    Debug.Print("    Index = " + swEqnMgr.Status);
                    //    Debug.Print("    Global variable? " + swEqnMgr.get_GlobalVariable(i));
                    //}

                    //修改高度为60

                    if (SetEquationValue(swEqnMgr, "h", 60))
                    {
                        swModel.ForceRebuild3(true);
                    }
                    else
                    {
                        MessageBox.Show("没有找到这个值!");
                    }
                }
            }
        }

        #region 修改全局变量所用到的方法

        public bool SetEquationValue(EquationMgr eqMgr, string name, double newValue)
        {
            int index = GetEquationIndexByName(eqMgr, name);

            if (index != -1)
            {
                eqMgr.Equation[index] = "\"" + name + "\"=" + newValue;

                return true;
            }
            else
            {
                return false;
            }
        }

        //通过名字找方程式的位置
        private int GetEquationIndexByName(EquationMgr eqMgr, string name)
        {
            int i;
            for (i = 0; i <= eqMgr.GetCount() - 1; i++)
            {
                var eqName = eqMgr.Equation[i].Split('=')[0].Replace("=", "");

                eqName = eqName.Substring(1, eqName.Length - 2); // removing the "" symbols from the name

                if (eqName.ToUpper() == name.ToUpper())
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion 修改全局变量所用到的方法

        private void btnCreateSketch_Click(object sender, EventArgs e)
        {
            //如果没有打开文件，请执行打开和创建的操作：
            //BtnOpenAndNew_Click(null, null);

            //连接到Solidworks
            ISldWorks swApp = Utility.ConnectToSolidWorks();

            ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

            //定义草图管理器
            SketchManager sketchManager = swModel.SketchManager;

            //按名字选择草图
            bool boolstatus = swModel.Extension.SelectByID2("Sketch1", "SKETCH", 0, 0, 0, false, 0, null, 0);

            if (boolstatus == true)
            {
                //编辑草图
                sketchManager.InsertSketch(false);
                //获取当前草图，当获取草图中的Segment对象
                Sketch sketch = swModel.GetActiveSketch2();
                object[] sketchSegments = sketch.GetSketchSegments();

                if (sketchSegments != null)
                {
                    //遍历
                    foreach (var skSeg in sketchSegments)
                    {
                        SketchSegment sketchSegment = (SketchSegment)skSeg;

                        //判断是直线时执行
                        if (sketchSegment.GetType() == (int)swSketchSegments_e.swSketchLINE)
                        {
                            SketchLine sketchLine = (SketchLine)sketchSegment;
                            SketchPoint sketchPointStart = sketchLine.GetStartPoint2();
                            SketchPoint sketchPointEnd = sketchLine.GetEndPoint2();

                            //这里显示弹出坐标，单位默认是米
                            MessageBox.Show(sketchPointStart.X.ToString() + "," + sketchPointStart.Y.ToString());
                            MessageBox.Show(sketchPointEnd.X.ToString() + "," + sketchPointEnd.Y.ToString());

                            SelectionMgr swSelMgr = swModel.SelectionManager;

                            //定义选择数据
                            SelectData swSelData = swSelMgr.CreateSelectData();

                            //选择此直线

                            sketchSegment.Select4(false, swSelData);

                            //删除当前的约束关系
                            swModel.SketchConstraintsDelAll();

                            //下在我们来修改坐标
                            sketchPointStart.X = 0.05;
                            sketchPointStart.Y = 0.04;

                            sketchPointEnd.X = 0.2;
                            sketchPointEnd.Y = 0.2;
                        }
                    }
                }

                //退出草图
                sketchManager.InsertSketch(true);
            }
        }

        private void btnSheetmetal_Click(object sender, EventArgs e)
        {
            //连接到Solidworks
            ISldWorks swApp = Utility.ConnectToSolidWorks();
            swApp.CommandInProgress = true;
            ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

            //钣金 变成平板模式的特征
            List<Feature> flatPatternFeatures = new List<Feature>();

            //Bounding Box草图
            List<string> boundingSketchesName = new List<string>();

            //获取当前钣金状态--这个已经过时

            //swSMBendStateFlattened  2 = 弯曲变平；该模型回滚到FlattenBends功能之后，但恰好在相应的ProcessBends功能之前
            //swSMBendStateFolded 3 = 折弯处已折叠；模型回滚到FlattenBends ProcessBends功能对之后
            //swSMBendStateNone   0 = 不是钣金零件；没有SheetMetal功能
            //swSMBendStateSharps 1 = 弯曲处处于锐利状态；零件回滚到第一个FlattenBends功能之前

            var bendState = swModel.GetBendState();

            if (bendState == 0)
            {
                swApp.SendMsgToUser("不是钣金零件！");
                return;
            }
            //  swApp.SendMsgToUser("当前状态" + bendState);
            if (bendState != 2)
            {
                //swApp.Command((int)swCommands_e.swCommands_Flatten, "");
                //设定当前钣金状态 平板 ，下面这行代码不适用现在的零件 ，只适用于很早之前的零件
                //var setStatus = swModel.SetBendState((int)swSMBendState_e.swSMBendStateFlattened);

                //新钣金均是通过获取零件
                var swFeatureManager = swModel.FeatureManager;
                var flatPatternFolder = (FlatPatternFolder)swFeatureManager.GetFlatPatternFolder();

                var featArray = (object[])flatPatternFolder.GetFlatPatterns();

                for (int i = featArray.GetLowerBound(0); i <= featArray.GetUpperBound(0); i++)
                {
                    var feat = (Feature)featArray[i];
                    Debug.Print("    " + feat.Name);

                    flatPatternFeatures.Add(feat);
                    feat.SetSuppression2((int)swFeatureSuppressionAction_e.swUnSuppressFeature, (int)swInConfigurationOpts_e.swThisConfiguration, null);

                    //解压子特征
                    var swSubFeat = (Feature)feat.GetFirstSubFeature();

                    while ((swSubFeat != null))
                    {
                        Debug.Print(swSubFeat.Name.ToString());
                        switch (swSubFeat.GetTypeName())
                        {
                            //如果是草图
                            case "ProfileFeature":

                                var sketchSpc = (Sketch)swSubFeat.GetSpecificFeature2();
                                object[] vSketchSeg = sketchSpc.GetSketchSegments();

                                for (int j = 0; j < vSketchSeg.Length; j++)
                                {
                                    SketchSegment swSketchSeg = (SketchSegment)vSketchSeg[j];

                                    //如果直线不是折弯线，说明是边界框
                                    if (swSketchSeg.IsBendLine() == false)
                                    {
                                        boundingSketchesName.Add(swSubFeat.Name);
                                    }
                                    else if (swSketchSeg.IsBendLine() == true)
                                    {
                                        Debug.Print("钣金宽度为:" + swSketchSeg.GetLength() * 1000);
                                    }
                                }

                                break;

                            default:
                                break;
                        }

                        swSubFeat = (Feature)swSubFeat.GetNextSubFeature();
                    }
                }

                swModel.EditRebuild3();
            }

            //遍历所有特征

            var swSelMgr = (SelectionMgr)swModel.SelectionManager;
            var swFeat = (Feature)swModel.FirstFeature();

            while ((swFeat != null))
            {
                //Debug.Print(swFeat.Name.ToString());
                // Process top-level sheet metal features
                switch (swFeat.GetTypeName())
                {
                    case "SMBaseFlange":
                        //var swBaseFlange = (BaseFlangeFeatureData)swFeat.GetDefinition();

                        //Debug.Print("钣金宽度为:" + swBaseFlange.D1OffsetDistance * 1000);

                        break;

                    case "SheetMetal":
                        //这里可以获取默认的厚度                        Debug.Print(swFeat.Name.ToString());
                        SheetMetalFeatureData sheetMetalFeatureData = (SheetMetalFeatureData)swFeat.GetDefinition();
                        Debug.Print("钣金默认厚度为:" + sheetMetalFeatureData.Thickness * 1000);

                        break;

                    case "SM3dBend":

                        break;

                    case "SMMiteredFlange":

                        break;
                }
                // process sheet metal sub-features
                var swSubFeat = (Feature)swFeat.GetFirstSubFeature();

                while ((swSubFeat != null))
                {
                    // Debug.Print(swSubFeat.Name.ToString());
                    switch (swSubFeat.GetTypeName())
                    {
                        case "SketchBend":

                            GetHisBendInformation(swApp, swModel, swSubFeat);
                            break;

                        case "OneBend":

                            GetHisBendInformation(swApp, swModel, swSubFeat);

                            break;

                        default:
                            break;
                            // Probably not a sheet metal feature
                    }

                    swSubFeat = (Feature)swSubFeat.GetNextSubFeature();
                }

                swFeat = (Feature)swFeat.GetNextFeature();
            }

            return;
        }

        private void GetHisBendInformation(ISldWorks swApp, ModelDoc2 swModel, Feature swFeat)
        {
            MathUtility swMathUtil = default(MathUtility);
            SelectionMgr swSelMgr = default(SelectionMgr);
            OneBendFeatureData swOneBend = default(OneBendFeatureData);
            Object[] vSketchSegs = null;
            SketchSegment swSketchSeg = default(SketchSegment);
            Sketch swSketch = default(Sketch);
            Feature swSketchFeat = default(Feature);
            SketchLine swSketchLine = default(SketchLine);
            SketchPoint swSkStartPt = default(SketchPoint);
            SketchPoint swSkEndPt = default(SketchPoint);
            SelectData swSelData = default(SelectData);
            double[] nPt = new double[3];
            MathPoint swStartPt = default(MathPoint);
            MathPoint swEndPt = default(MathPoint);
            MathTransform swSkXform = default(MathTransform);
            int[] vID = null;
            int i = 0;

            swMathUtil = (MathUtility)swApp.GetMathUtility();

            swSelMgr = (SelectionMgr)swModel.SelectionManager;
            //swFeat = (Feature)swSelMgr.GetSelectedObject6(1, -1);
            //swSelData = swSelMgr.CreateSelectData();
            swOneBend = (OneBendFeatureData)swFeat.GetDefinition();

            /*swBaseBend 4
            swEdgeFlangeBend 8
            swFlat3dBend 6
            swFlatBend 2
            swFreeFormBend 10 = Obsolete
            swHemBend 9
            swLoftedBend 12
            swMirrorBend 7
            swMiterBend 5
            swNoneBend 3
            swRoundBend 1
            swRuledBend 11 = Obsolete
            swSharpBend 0
            */

            Debug.Print("Type of bend (swBendType_e): " + swOneBend.GetType());
            Debug.Print("折弯次数: " + swOneBend.GetFlatPatternSketchSegmentCount2());
            Debug.Print("折弯序号: " + swOneBend.BendOrder);
            Debug.Print("折弯角度: " + swOneBend.BendAngle * 57.3 + " deg");
            Debug.Print("折弯圆角: " + swOneBend.BendRadius);

            if (swOneBend.BendDown == true)
            {
                Debug.Print("向下折弯: " + "Yes");
            }
            else
            {
                Debug.Print("向下折弯: " + " No");
            }

            vSketchSegs = (Object[])swOneBend.FlatPatternSketchSegments2;

            for (i = 0; i <= vSketchSegs.GetUpperBound(0); i++)
            {
                swSketchSeg = (SketchSegment)vSketchSegs[i];
                swSketch = swSketchSeg.GetSketch();
                swSketchLine = (SketchLine)swSketchSeg;
                swSkStartPt = (SketchPoint)swSketchLine.GetStartPoint2();
                swSkEndPt = (SketchPoint)swSketchLine.GetEndPoint2();
                vID = (int[])swSketchSeg.GetID();

                // Get sketch feature
                swSketchFeat = (Feature)swSketch;
                swSkXform = swSketch.ModelToSketchTransform;
                swSkXform = (MathTransform)swSkXform.Inverse();

                nPt[0] = swSkStartPt.X;
                nPt[1] = swSkStartPt.Y;
                nPt[2] = swSkStartPt.Z;
                swStartPt = (MathPoint)swMathUtil.CreatePoint(nPt);
                swStartPt = (MathPoint)swStartPt.MultiplyTransform(swSkXform);
                double[] swStartPtArrayData;
                swStartPtArrayData = (double[])swStartPt.ArrayData;

                nPt[0] = swSkEndPt.X;
                nPt[1] = swSkEndPt.Y;
                nPt[2] = swSkEndPt.Z;
                swEndPt = (MathPoint)swMathUtil.CreatePoint(nPt);
                swEndPt = (MathPoint)swEndPt.MultiplyTransform(swSkXform);
                double[] swEndPtArrayData;
                swEndPtArrayData = (double[])swEndPt.ArrayData;

                // Debug.Print("File = " + swModel.GetPathName());
                Debug.Print("  Feature = " + swFeat.Name + " [" + swFeat.GetTypeName2() + "]");
                Debug.Print("    Sketch             = " + swSketchFeat.Name);
                Debug.Print("    SegID              = [" + vID[0] + ", " + vID[1] + "]");
                Debug.Print("    Start with respect to sketch   = (" + swSkStartPt.X * 1000.0 + ", " + swSkStartPt.Y * 1000.0 + ", " + swSkStartPt.Z * 1000.0 + ") mm");
                Debug.Print("    End with respect to sketch   = (" + swSkEndPt.X * 1000.0 + ", " + swSkEndPt.Y * 1000.0 + ", " + swSkEndPt.Z * 1000.0 + ") mm");
                Debug.Print("    Start with respect to model    = (" + swStartPtArrayData[0] * 1000.0 + ", " + swStartPtArrayData[1] * 1000.0 + ", " + swStartPtArrayData[2] * 1000.0 + ") mm");
                Debug.Print("    End with respect to model    = (" + swEndPtArrayData[0] * 1000.0 + ", " + swEndPtArrayData[1] * 1000.0 + ", " + swEndPtArrayData[2] * 1000.0 + ") mm");
            }
        }

        private void btnGetDimensionInfo_Click(object sender, EventArgs e)
        {
            ISldWorks swApp = Utility.ConnectToSolidWorks();
            swApp.CommandInProgress = true;

            ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

            SelectionMgr selectionMgr = (SelectionMgr)swModel.SelectionManager;

            //转换成尺寸显示对象
            var swDisplayDimension = (DisplayDimension)selectionMgr.GetSelectedObject6(1, 0);

            DisplayData displayData = (DisplayData)swDisplayDimension.GetDisplayData();

            //获取尺寸上的文字
            var anno = (Annotation)swDisplayDimension.GetAnnotation();

            //获取所在视图  ---如果是图纸，这里会报错。需要用OwnerType来判断
            var thisView = (View)anno.Owner;//

            var textwidth = displayData.GetTextInBoxWidthAtIndex(0);

            var textHeight = displayData.GetTextHeightAtIndex(0);

            // dat.GetLineCount 几条线
            var lineCount = displayData.GetLineCount();
            var lineAngle = displayData.GetTextAngleAtIndex(0);
            var linePoints = (double[])displayData.GetLineAtIndex(0);
            var linePoints2 = (double[])displayData.GetLineAtIndex(1);
            var textPoint = (double[])displayData.GetTextPositionAtIndex(0);

            var thisDimAng = lineAngle * 180 / Math.PI;

            //尺寸对象
            var swDimension = (Dimension)swDisplayDimension.GetDimension();

            //获取尺寸的公差
            var cruToleranceType = swDimension.GetToleranceType();
            var cruTolerance = swDimension.Tolerance;

            if (cruToleranceType == (int)swTolType_e.swTolBILAT)
            {
                cruTolerance.GetMaxValue2(out double ToleranceValueMax); //上公差

                cruTolerance.GetMinValue2(out double ToleranceValueMin);//下公差
            }

            var TextAll = swDisplayDimension.GetText((int)swDimensionTextParts_e.swDimensionTextAll);
            var TextPrefix = swDisplayDimension.GetText((int)swDimensionTextParts_e.swDimensionTextPrefix);
            var TextSuffix = swDisplayDimension.GetText((int)swDimensionTextParts_e.swDimensionTextSuffix);
            var CalloutAbove = swDisplayDimension.GetText((int)swDimensionTextParts_e.swDimensionTextCalloutAbove);
            var CalloutBelow = swDisplayDimension.GetText((int)swDimensionTextParts_e.swDimensionTextCalloutBelow);

            var relValue = Math.Round(swDimension.GetSystemValue2("") * 1000, 3).ToString();

            MessageBox.Show(relValue);
        }
    }
}