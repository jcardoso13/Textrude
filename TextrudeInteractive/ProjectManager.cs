﻿using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Engine.Application;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using SharedApplication;

namespace TextrudeInteractive
{
    public class ProjectManager
    {
        private const string Filter = "project files (*.texproj)|*.texproj|All files (*.*)|*.*";
        private readonly MainWindow _owner;

        private string _currentProjectPath = string.Empty;

        public ProjectManager(MainWindow owner) => _owner = owner;


        private TextrudeProject CreateProject()
        {
            var proj = new TextrudeProject
            {
                EngineInput = _owner.CollectInput(),
                OutputControl = _owner.CollectOutput()
            };

            return proj;
        }

        public void LoadProject()
        {
            var dlg = new OpenFileDialog {Filter = Filter};

            if (dlg.ShowDialog(_owner) == true)
            {
                try
                {
                    var text = File.ReadAllText(dlg.FileName);
                    var proj = JsonSerializer.Deserialize<TextrudeProject>(text);
                    _currentProjectPath = dlg.FileName;
                    UpdateUI(proj);
                }
                catch
                {
                    MessageBox.Show(_owner, "Error - unable to open project");
                }
            }
        }

        public void SaveProject()
        {
            if (string.IsNullOrWhiteSpace(_currentProjectPath))
                SaveProjectAs();
            else
            {
                try
                {
                    var o = new JsonSerializerOptions {WriteIndented = true};
                    var text = JsonSerializer.Serialize(CreateProject(), o);

                    File.WriteAllText(_currentProjectPath, text);
                }
                catch
                {
                    MessageBox.Show(_owner, "Error - unable to save project");
                }
            }
        }

        public void SaveProjectAs()
        {
            var dlg = new SaveFileDialog {Filter = Filter};
            if (dlg.ShowDialog(_owner) == true)
            {
                _currentProjectPath = dlg.FileName;
                _owner.SetTitle(_currentProjectPath);
                SaveProject();
            }
        }

        public void NewProject()
        {
            _currentProjectPath = string.Empty;
            var proj = new TextrudeProject();
            UpdateUI(proj);
        }

        private void UpdateUI(TextrudeProject project)
        {
            _owner.SetUi(project.EngineInput);
            _owner.SetTitle(_currentProjectPath);
            _owner.SetOutputPanes(project.OutputControl);
        }

        public void ExportProject()
        {
            var dlg = new VistaFolderBrowserDialog();
            if (dlg.ShowDialog(_owner) == false)
            {
            }
        }
    }

    public class InvocationManager
    {
        private const string exeName = "textrude.exe";
        private readonly string _folder;
        private readonly TextrudeProject _project;

        public InvocationManager(TextrudeProject project, string folder)
        {
            _project = project;
            _folder = folder;
        }

        public void BuildCommandLine()
        {
            var engine = _project.EngineInput;
            var options = new RenderOptions {Definitions = engine.Definitions, Include = engine.IncludePaths};
            var exe =
                Path.Combine(new RunTimeEnvironment(new FileSystemOperations()).ApplicationFolder(),
                    exeName);
            options.Models = engine.Models.Select(m => m.Path).ToArray();
            options.Template = engine.TemplatePath;
            options.Output = _project.OutputControl.Outputs.Select(o => o.Path).ToArray();
            var builder = new CommandLineBuilder(options).WithExe(exe);
        }

        private void WriteToFile(string fileName, string content)
            => File.WriteAllText(Path.Combine(_folder, fileName), content);

        public void ExportToFolder()
        {
            try
            {
                var engine = _project.EngineInput;
                var options = new RenderOptions {Definitions = engine.Definitions, Include = engine.IncludePaths};


                for (var i = 0; i < engine.Models.Length; i++)
                {
                    var m = engine.Models[i];
                    if (m.Text.Trim().Length == 0)
                    {
                        continue;
                    }

                    var mName = Path.ChangeExtension($"model{i}", m.Format.ToString());
                    WriteToFile(mName, m.Text);
                    options.Models = options.Models.Append(mName).ToArray();
                }

                var templateName = "template.sbn";
                WriteToFile(templateName, engine.Template);
                options.Template = templateName;

                var exe =
                    Path.Combine(new RunTimeEnvironment(new FileSystemOperations()).ApplicationFolder(),
                        exeName);

                var builder = new CommandLineBuilder(options).WithExe(exe);

                WriteToFile("render.bat", $"{builder.BuildRenderInvocation()}");


                //write yaml invocation

                var jsonArgs = "args.json";
                var (json, jsoncmd) = builder.BuildJson(jsonArgs);
                WriteToFile(jsonArgs, json);
                WriteToFile("jsonrender.bat", jsoncmd);


                var yamlArgs = "args.yaml";
                var (yaml, yamlCmd) = builder.BuildYaml(yamlArgs);
                WriteToFile(yamlArgs, yaml);
                WriteToFile("yamlrender.bat", yamlCmd);
            }
            catch
            {
                MessageBox.Show("Sorry - couldn't export invocation");
            }
        }
    }
}