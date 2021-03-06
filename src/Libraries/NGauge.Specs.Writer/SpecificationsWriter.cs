﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NGauge.CodeContracts;
using NGauge.Specs.Writer.Services;

namespace NGauge.Specs.Writer
{
    internal sealed class SpecificationsWriter : ISpecificationsWriter
    {
        private readonly IGeneratedCodeNamingService _generatedCodeNamingService;
        private readonly ISpecificationCodeGenerator _specificationCodeGenerator;
        private readonly IFolderDeletionService _folderDeletionService;
        private readonly ICodeSavingService _codeSavingService;

        public SpecificationsWriter(IGeneratedCodeNamingService generatedCodeNamingService,
            ISpecificationCodeGenerator specificationCodeGenerator, IFolderDeletionService folderDeletionService,
            ICodeSavingService codeSavingService)
        {
            Contract.RequiresNotNull(generatedCodeNamingService, nameof(generatedCodeNamingService));
            Contract.RequiresNotNull(specificationCodeGenerator, nameof(specificationCodeGenerator));
            Contract.RequiresNotNull(folderDeletionService, nameof(folderDeletionService));
            Contract.RequiresNotNull(codeSavingService, nameof(codeSavingService));

            _generatedCodeNamingService = generatedCodeNamingService;
            _specificationCodeGenerator = specificationCodeGenerator;
            _folderDeletionService      = folderDeletionService;
            _codeSavingService          = codeSavingService;
        }

        async Task<string> ISpecificationsWriter.WriteSpecificationsAsync(IEnumerable<ISpecification> specifications, string projectPath)
        {
            Contract.RequiresNotNull(specifications, nameof(specifications));
            Contract.RequiresNotNull(projectPath, nameof(projectPath));

            var generatedCodePath = _generatedCodeNamingService.GetGeneratedCodePath(projectPath);

            _folderDeletionService.Delete(generatedCodePath);

            var specificationWritingTasks = specifications
                .AsParallel()
                .Select(specification => Task.Run(() => _specificationCodeGenerator.GenerateCode(specification)));

            await Task.WhenAll(specificationWritingTasks);

            foreach (var generatedCode in specificationWritingTasks.Select(task => task.Result))
            {
                _codeSavingService.Save(generatedCode, generatedCodePath);
            }

            return generatedCodePath;
        }
    }
}
