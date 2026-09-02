using System.Collections.Generic;
using UnityEditor.Compilation;

namespace FieldDay.Editor {
    static public class BuildLinter {
        static private Assembly[] GatherAllBuildAssemblies() {
            return CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
        }

        static private void GatherFilePaths(Assembly assembly, List<string> filePaths) {
            filePaths.AddRange(assembly.sourceFiles); 
        }

        // TODO: Check for Linq and other forbidden references
    }
}