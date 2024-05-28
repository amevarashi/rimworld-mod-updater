using System;

namespace RimWorldModUpdater.Models;

public class ModSourcesException(string message) : Exception(message)
{
}