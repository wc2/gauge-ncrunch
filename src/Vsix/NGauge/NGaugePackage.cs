﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Autofac;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using NGauge.Bridge;
using NGauge.Extensions;
using NGauge.Specs.Reader;
using NGauge.Specs.Writer.xUnit;

namespace NGauge
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad("f1536ef8-92ec-443c-9ed7-fdadf150da82")] // Microsoft.VisualStudio.VSConstants.UICONTEXT_SolutionExists
    public sealed class NGaugePackage : Package
    {
        public const string PackageGuidString = "5576bd85-154f-4d43-97e3-5de72ebf4e43";
        private IGenerator _generator;

        public NGaugePackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for class {0}.", GetType().Name));
        }

        protected override void Initialize()
        {
            InitialiseBridgingComponents();
            MonitorGaugeDocuments();

            base.Initialize();
        }

        private void InitialiseBridgingComponents()
        {
            var container = GetDependencyResolutionContainer();
            using (var scope = container.BeginLifetimeScope())
            {
                _generator = scope.Resolve<IGenerator>();
            }
        }

        private static IContainer GetDependencyResolutionContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule<BridgeModule>();
            builder.RegisterModule<xUnitModule>();
            builder.RegisterInstance(new GaugeSpecificationsService(1234)).As<IGaugeSpecificationsService>();

            return builder.Build();
        }

        private void MonitorGaugeDocuments()
        {
            var environment = (DTE)GetService(typeof(DTE));
            var documentEvents = environment.Events.DocumentEvents;

            documentEvents.DocumentSaved += RebuildGaugeBridgeToNCrunchIfEditedDocumentIsGaugeDocument;
        }

        private void RebuildGaugeBridgeToNCrunchIfEditedDocumentIsGaugeDocument(Document document)
        {
            if (document.IsGaugeDocument())
            {
                _generator
                    .CreateOrUpdateAsync(document.GetProjectPath())
                    .ContinueWith(
                        createOrUpdate =>
                            document.ProjectItem.ContainingProject.EnsureFileIsReferenced(createOrUpdate.Result));

            }
        }
    }
}