using System.Runtime.CompilerServices;
using Roag.Reference;

namespace Roag.Core.Tests;

internal static class PrototypeReferenceSetup
{
    [ModuleInitializer]
    internal static void InstallArchivedPrototype() => PrototypeReference.Install();
}
