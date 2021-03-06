﻿using BlackBerry.Package.ViewModels;
#if PLATFORM_VS2010
using Microsoft.VisualStudio.Project.Contracts.PropertyPages.VS2010ONLY;
#elif PLATFORM_VS2012
using Microsoft.VisualStudio.Project.Designers.Properties;
using Microsoft.VisualStudio.Project.Properties;
#elif PLATFORM_VS2013 || PLATFORM_VS2015
using Microsoft.VisualStudio.ProjectSystem.Designers.Properties;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.Threading.Tasks;
#endif
using System.Collections.Generic;
using System.IO;

namespace BlackBerry.Package.MSBuildExtensions.ValueProviders
{
    /// <summary>
    /// Generator for values into dynamic-enum.
    /// Scans the active NDK and populates list with all available versions of the compiler.
    /// </summary>
    sealed class CompilerVersionsGenerator : IDynamicEnumValuesGenerator
    {
        public bool AllowCustomValues
        {
            get { return false; }
        }

#if PLATFORM_VS2010 || PLATFORM_VS2012
        public bool TryCreateEnumValue(string userSuppliedValue, out IEnumValue value)
        {
            value = null;
            return false;
        }

        public ICollection<IEnumValue> ListedValues
        {
            get { return LoadList(); }
        }
#endif

#if PLATFORM_VS2013 || PLATFORM_VS2015
        public Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            return Task.FromResult<ICollection<IEnumValue>>(LoadList());
        }

        public Task<IEnumValue> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            return Task.FromResult<IEnumValue>(null);
        }
#endif

        private static IEnumValue[] LoadList()
        {
            var result = new List<IEnumValue>();
            var ndk = PackageViewModel.Instance != null ? PackageViewModel.Instance.ActiveNDK : null;

            // add the default item, to let the toolset select appropriate the compiler version:
            result.Add(new DynamicEnumValue("", "Default", "Default compiler version evaluated and used by toolset", true));

            if (ndk != null && Directory.Exists(ndk.HostPath))
            {
                var dirs = Directory.GetDirectories(Path.Combine(ndk.HostPath, "etc", "qcc", "gcc"));
                foreach (var d in dirs)
                {
                    var name = Path.GetFileName(d);
                    result.Add(new DynamicEnumValue(name, name, string.Concat("Forces usage of the '", name, "' version of the compiler (", d, ")"), false));
                }
            }

            return result.ToArray();
        }
    }
}
