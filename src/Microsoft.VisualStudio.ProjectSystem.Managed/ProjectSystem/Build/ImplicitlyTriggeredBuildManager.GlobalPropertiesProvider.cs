﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Managed.Build
{
    internal sealed partial class ImplicitlyTriggeredBuildManager
    {
        /// <summary>
        /// Global properties provider for implicitly triggered builds from commands such as Run/Debug Tests, Start Debugging, etc.
        /// that should skip running analyzers in order to reduce build times.
        /// This provider does not affect the property collection for design time builds.
        /// </summary>
        /// <remarks>
        /// Currently, the provider is only for CPS based SDK-style projects, not for legacy csproj projects.
        /// https://github.com/dotnet/project-system/issues/7346 tracks implementing the project system support for legacy csproj projects.
        /// </remarks>
        [ExportBuildGlobalPropertiesProvider]
        [AppliesTo(ProjectCapability.DotNet)]
        private sealed class GlobalPropertiesProvider : StaticGlobalPropertiesProviderBase
        {
            private readonly IImplicitlyTriggeredBuildState _implicitlyTriggeredBuildState;
            private readonly IProjectSystemOptions _projectSystemOptions;

            /// <summary>
            /// Initializes a new instance of the <see cref="GlobalPropertiesProvider"/> class.
            /// </summary>
            [ImportingConstructor]
            public GlobalPropertiesProvider(UnconfiguredProject unconfiguredProject,
                IImplicitlyTriggeredBuildState implicitlyTriggeredBuildState,
                IProjectSystemOptions projectSystemOptions)
                : base(unconfiguredProject.Services)
            {
                _implicitlyTriggeredBuildState = implicitlyTriggeredBuildState;
                _projectSystemOptions = projectSystemOptions;
            }

            /// <summary>
            /// Gets the set of global properties that should apply to the project(s) in this scope.
            /// </summary>
            /// <value>A new dictionary whose keys are case insensitive.  Never null, but may be empty.</value>
            public override async Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
            {
                bool useImplicitlyTriggeredBuildProperties = _implicitlyTriggeredBuildState.IsImplicitlyTriggeredBuild
                    && await _projectSystemOptions.GetSkipAnalyzersForImplicitlyTriggeredBuildAsync(cancellationToken);

                ImmutableDictionary<string, string> globalProperties = useImplicitlyTriggeredBuildProperties
                    ? GlobalPropertiesStore.Instance.GetImplicitlyTriggeredBuildProperties()
                    : GlobalPropertiesStore.Instance.GetRegularBuildProperties();

                return globalProperties;
            }
        }
    }
}
