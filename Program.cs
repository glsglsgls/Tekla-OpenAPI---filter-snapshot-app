using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model.UI;




namespace myspace
{
    class Myclass
    {
        static void Main()
        {   
            MessageBox.Show("This app will create snapshot for all listed PHASES from Tekla\n\nPAY ATTENTION!\nYou must open the VIEW PROPERTIES window BEFORE execute the macro");
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new Form1());
        }
        static string Path;
        static StreamWriter file;
        static string Screenpath;
        static string Filename;
        static Model CurrentModel;

        internal static string GetPhasesNumbersFromTekla(string DP) //Function: input - DP numbers, like '1,2,3,5'; output Phase numbers from opened Tekla model
        {   CurrentModel = new Model();
            if (!CurrentModel.GetConnectionStatus()) {MessageBox.Show("Tekla connection not detected"); string nothing=""; return nothing;}
            String[] ExceptedPHASEcomment = {"Flooring", "Plating"};
            String[] ExceptedPHASEname = {"DUMMY", "abcdefblank"};
            String[] DParray = DP.Split(',');
            DParray.ToList<string>();
            PhaseCollection PhaseCollection = CurrentModel.GetPhases();
            Dictionary<string, string> dic = new Dictionary<string, string>();
            foreach(Phase p in PhaseCollection)
            {   if(ExceptedPHASEname.Any(Convert.ToString(p.PhaseName).Contains)) {continue;}
                string DPphaseNumbers=Convert.ToString(p.PhaseNumber);
                string phasename = "";
                p.GetUserProperty("PHASE_SSDP",ref phasename);
                phasename = phasename.Trim(new Char[] { 'D', 'P' });
                if(!DParray.Contains(phasename)) {continue;}
                string comment1Phase = "";
                p.GetUserProperty("comment",ref comment1Phase);
                if(ExceptedPHASEcomment.Any(comment1Phase.Contains)) {continue;}
                foreach(Phase secondp in PhaseCollection)
                {   if(ExceptedPHASEname.Any(Convert.ToString(secondp.PhaseName).Contains)) {continue;}
                    string secondphasename = "";
                    secondp.GetUserProperty("PHASE_SSDP",ref secondphasename);
                    secondphasename = secondphasename.Trim(new Char[] { 'D', 'P' });
                    if(!DParray.Contains(secondphasename)) {continue;}
                    string comment2Phase = "";
                    secondp.GetUserProperty("comment",ref comment2Phase);
                    //MessageBox.Show($"phase {p.PhaseNumber} is compared with {secondp.PhaseNumber}\n1 comment {comment1Phase}; 2 comment {comment2Phase}\n");
                    if(ExceptedPHASEcomment.Any(comment2Phase.Contains)) {continue;}
                    if(secondp.PhaseNumber != p.PhaseNumber && phasename == secondphasename) {DPphaseNumbers = DPphaseNumbers + " " + Convert.ToString(secondp.PhaseNumber);}
                }
                if(!dic.ContainsKey(phasename)) {dic.Add(phasename,DPphaseNumbers);}
            }
            string PhaseList = "";
            if(DParray.Count()>1)
            {   foreach(string P in DParray)
                {   string phasesINdp = "";
                    if(dic.ContainsKey(P)) {if(phasesINdp == "") {phasesINdp = dic[P];} else {phasesINdp = phasesINdp + " " + dic[P];}   }
                    if(PhaseList == "") {PhaseList = phasesINdp;} else {PhaseList = PhaseList + '-' + phasesINdp;}
                }
            }
            else if(DParray.Count()==0) {PhaseList="";}
            else {if(dic.ContainsKey(DParray[0])) {PhaseList = dic[DParray[0]];} else {PhaseList = "";}}
            return PhaseList;
        }
                static Tekla.Structures.Model.ModelObjectEnumerator GetAllBeamObjects()
        {   Model Model = new Model();
            Tekla.Structures.Model.ModelObjectEnumerator myEnum = Model.GetModelObjectSelector().GetAllObjectsWithType(Tekla.Structures.Model.ModelObject.ModelObjectEnum.BEAM);
            return myEnum;
        }
        internal static string GetDPnameFromPhaseNumber(string PhaseNumber)
        {   PhaseCollection PhaseCollection = CurrentModel.GetPhases();
            string phasename = "";
            if(PhaseNumber !="")
            {   foreach(Phase p in PhaseCollection)
                {   if (Convert.ToUInt32(PhaseNumber) != p.PhaseNumber) {continue;}
                    p.GetUserProperty("PHASE_SSDP",ref phasename);
                }
            }
            else {phasename="";}
            return phasename;
        }
        internal static void OneSnapshotPerScript(string filename, string scriptpath, string screenpath, string dp, bool keepDP)
        {   Directory.CreateDirectory(screenpath);
            Path = scriptpath;
            Filename = filename;
            Screenpath = screenpath;
            String[] DParray = dp.Split(',');
            int count=1;
            string DPlist = "";
            //MessageBox.Show(Convert.ToString(DParray.Count()));
            foreach(string DP in DParray)
            {   string Phases = GetPhasesNumbersFromTekla(DP);
                DPlist += " " + Phases;
                Createfile();
                file.WriteLine();
                file.WriteLine("//filter phase");
                if(keepDP) {TextFilter(DPlist);}
                else {TextFilter(Phases);}
                string[] getFirstPhaseFromPhaseArray = Phases.Split(' '); string Phasenum = getFirstPhaseFromPhaseArray[0];
                file.WriteLine("//do screenshot");
                TextScreenshot(Phases,Convert.ToString(count) + "-DP" + DP);
                Closefile();
                Tekla.Structures.Model.Operations.Operation.RunMacro(filename);
                count+=1;
            }
            Process.Start(Screenpath.Replace("\\\\", "\\"));
        }
        internal static void AllSnapshotPerScript(string filename, string scriptpath, string screenpath, string dp, bool keepDP)
        {   Path = scriptpath;
            Filename = filename;
            Screenpath = screenpath;
            String PhaseList = GetPhasesNumbersFromTekla(dp);
            String[] Phasearray = PhaseList.Split('-');
            Directory.CreateDirectory(Screenpath);
            Createfile();
            string DPlist = "";
            //int j=1; foreach(string p in Phasearray) {MessageBox.Show($"\n DPnumber {j}; Phases in current DP {p}"); j+=1;}
            int count=1;
            for (int i = 0; i < Phasearray.Length; i++)
            {   DPlist += " " + Phasearray[i];
                file.WriteLine();
                file.WriteLine("//filter phase");
                if(keepDP) {TextFilter(DPlist);}
                else {TextFilter(Phasearray[i]);}
                file.WriteLine("//do screenshot");
                string[] getFirstPhaseFromPhaseArray = Phasearray[i].Split(' '); string DP = GetDPnameFromPhaseNumber(getFirstPhaseFromPhaseArray[0]);
                TextScreenshot(DPlist,Convert.ToString(count) + "-" + DP);
                count+=1;
            }
            
            Closefile();
            Tekla.Structures.Model.Operations.Operation.RunMacro(filename);
            Process.Start(Screenpath.Replace("\\\\", "\\"));
        }    
        
        static void CreateSettingsfile()
        {   file = new StreamWriter(Path + "\\" + "setting" + Filename);
            file.WriteLine("#pragma warning disable 1633 // Unrecognized #pragma directive");
            file.WriteLine("#pragma reference \"Tekla.Macros.Wpf.Runtime\"");
            file.WriteLine("#pragma reference \"Tekla.Macros.Akit\"");
            file.WriteLine("#pragma reference \"Tekla.Macros.Runtime\"");
            file.WriteLine("#pragma warning restore 1633 // Unrecognized #pragma directive");
            file.WriteLine("namespace UserMacros {");
            file.WriteLine("    public sealed class Macro {");
            file.WriteLine("        [Tekla.Macros.Runtime.MacroEntryPointAttribute()]");
            file.WriteLine("        public static void Run(Tekla.Macros.Runtime.IMacroRuntime runtime) {");
            file.WriteLine("			Tekla.Macros.Akit.IAkitScriptHost akit = runtime.Get<Tekla.Macros.Akit.IAkitScriptHost>();");
            file.WriteLine("			Tekla.Macros.Wpf.Runtime.IWpfMacroHost wpf = runtime.Get<Tekla.Macros.Wpf.Runtime.IWpfMacroHost>();");
            file.WriteLine("            akit.PushButton(\"v1_filter\", \"dia_view_dialog\");");
            file.WriteLine("            akit.PushButton(\"NewButton\", \"diaViewObjectGroup\");");
            file.WriteLine("            akit.PushButton(\"pushbutton_5154\", \"diaViewObjectGroup\");");
            file.WriteLine("            akit.TableSelect(\"diaViewObjectGroup\", \"RuleTable\", new int[] {1});");
            file.WriteLine("            akit.TableValueChange(\"diaViewObjectGroup\", \"RuleTable\", \"CheckBox\", \"1\");");
            file.WriteLine("            akit.TableValueChange(\"diaViewObjectGroup\", \"RuleTable\", \"Category\", \"co_object\");");
            file.WriteLine("            akit.TableValueChange(\"diaViewObjectGroup\", \"RuleTable\", \"Property\", \"albl_Phase\");");
            file.WriteLine("            akit.TableValueChange(\"diaViewObjectGroup\", \"RuleTable\", \"Value\", \"31\");");
            file.WriteLine("            akit.PushButton(\"attrib_saveas\", \"diaViewObjectGroup\");");
            file.WriteLine("            akit.PushButton(\"dia_pa_modify\", \"diaViewObjectGroup\");");
            file.WriteLine("            akit.PushButton(\"dia_pa_ok\", \"diaViewObjectGroup\");");
            file.WriteLine("            akit.ValueChange(\"dia_view_dialog\", \"view_filter\", \"newFilter\");");
            file.WriteLine("            akit.PushButton(\"v1_modify\", \"dia_view_dialog\");");
            file.WriteLine("        }");
            file.WriteLine("    }");
            file.WriteLine("}");
            file.Close();
            Tekla.Structures.Model.Operations.Operation.RunMacro("setting" + Filename);
        }
        
        
        static void Createfile()
        {   file = new StreamWriter(Path + "\\" + Filename);
            file.WriteLine("#pragma warning disable 1633 // Unrecognized #pragma directive");
            file.WriteLine("#pragma reference \"Tekla.Macros.Wpf.Runtime\"");
            file.WriteLine("#pragma reference \"Tekla.Macros.Akit\"");
            file.WriteLine("#pragma reference \"Tekla.Macros.Runtime\"");
            file.WriteLine("#pragma warning restore 1633 // Unrecognized #pragma directive");
            file.WriteLine("namespace UserMacros {");
            file.WriteLine("    public sealed class Macro {");
            file.WriteLine("        [Tekla.Macros.Runtime.MacroEntryPointAttribute()]");
            file.WriteLine("        public static void Run(Tekla.Macros.Runtime.IMacroRuntime runtime) {");
            file.WriteLine("			Tekla.Macros.Akit.IAkitScriptHost akit = runtime.Get<Tekla.Macros.Akit.IAkitScriptHost>();");
            file.WriteLine("			Tekla.Macros.Wpf.Runtime.IWpfMacroHost wpf = runtime.Get<Tekla.Macros.Wpf.Runtime.IWpfMacroHost>();");

        }
        static void Closefile()
        {   file.WriteLine("        }");
            file.WriteLine("    }");
            file.WriteLine("}");
            file.Close();
        }
        static void TextFilter(string Dp)
        {   file.WriteLine("			akit.PushButton(\"v1_filter\", \"dia_view_dialog\");");
            file.WriteLine("            akit.TableSelect(\"diaViewObjectGroup\", \"RuleTable\", new int[] {1});");
            file.WriteLine("            akit.TableValueChange(\"diaViewObjectGroup\", \"RuleTable\", \"Value\", \"" + Dp + "\");");
            file.WriteLine("            akit.PushButton(\"dia_pa_modify\", \"diaViewObjectGroup\");");
            file.WriteLine("            akit.PushButton(\"dia_pa_ok\", \"diaViewObjectGroup\");");
        }
        static void TextScreenshot(string Dp, string count)
        {   file.WriteLine("            wpf.InvokeCommand(\"CommandRepository\", \"Tools.Screenshot\");");
            file.WriteLine("            akit.ValueChange(\"snapshot_dialog\", \"target_selection\", \"1\");");
            file.WriteLine("            akit.ValueChange(\"snapshot_dialog\", \"filename\", \"" + Screenpath + "Image" + count + ".png\");");  
            file.WriteLine("                        akit.ModalDialog(1);");
            file.WriteLine("            akit.PushButton(\"take_snapshot\", \"snapshot_dialog\");");
            file.WriteLine("			akit.PushButton(\"cancel\", \"snapshot_dialog\");");
        }
    }
}
