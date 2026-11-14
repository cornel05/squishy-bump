using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>Guards the GDD 6.1 engine settings and the Core dependency rule (ADR-0003).</summary>
    public class EngineSettingsTests
    {
        [Test]
        public void FixedTimestep_Is60Hz() => Assert.AreEqual(1f / 60f, Time.fixedDeltaTime, 1e-6f);

        [Test]
        public void Gravity_IsMinus20() => Assert.AreEqual(-20f, Physics.gravity.y, 1e-4f);

        [Serializable] class Asmdef { public string[] references = new string[0]; }

        [Test]
        public void Core_ReferencesNoOtherSquishyBumpAssembly()
        {
            var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName("SquishyBump.Core");
            Assert.IsNotNull(path, "SquishyBump.Core asmdef not found");
            var refs = JsonUtility.FromJson<Asmdef>(File.ReadAllText(path)).references;
            var bad = refs.Where(r => r.StartsWith("SquishyBump.")).ToArray();
            Assert.IsEmpty(bad, "Core must not reference: " + string.Join(", ", bad));
        }
    }
}
