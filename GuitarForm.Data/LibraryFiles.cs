using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GuitarForm.Data
{
    // The files that belong beside a library and travel with it when it is moved: the safety copies an upgrade makes
    // ("<name>.before-v{N}.bak", ".1.bak", ".2.bak" ...) and the upgrade notes ("<name>.before-v{N}.notes.txt", with
    // the same numbering). See Backups.SafetyCopy and Backups.WriteNotes. No Rhino dependency.
    public static class LibraryFiles
    {
        // The safety copies and notes files beside the library (full paths, sorted), whether or not the library file
        // itself exists. Other libraries' files and unrelated files are not included.
        public static IReadOnlyList<string> SiblingsOf(string libraryPath)
        {
            string full = Path.GetFullPath(libraryPath);
            string folder = Path.GetDirectoryName(full);
            if (!Directory.Exists(folder))
                return Array.Empty<string>();

            var pattern = new Regex(
                "^" + Regex.Escape(Path.GetFileNameWithoutExtension(full)) + @"\.before-v\d+(\.\d+)?\.(bak|notes\.txt)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return Directory.GetFiles(folder)
                .Where(path => pattern.IsMatch(Path.GetFileName(path)))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Moves the library file and its siblings to the destination's folder (the siblings take the destination's
        // name stem). Nothing is overwritten: if any destination name is already taken, nothing is moved and an
        // IOException names it. If a move fails part way, the files already moved are put back. Returns false, having
        // done nothing, if there is no library file at `from`.
        public static bool Move(string from, string to)
        {
            string source = Path.GetFullPath(from);
            string destination = Path.GetFullPath(to);
            if (!File.Exists(source))
                return false;
            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                return true;

            string oldStem = Path.GetFileNameWithoutExtension(source);
            string newStem = Path.GetFileNameWithoutExtension(destination);
            string destinationFolder = Path.GetDirectoryName(destination);

            var plan = new List<(string From, string To)> { (source, destination) };
            foreach (string sibling in SiblingsOf(source))
            {
                string name = Path.GetFileName(sibling);
                plan.Add((sibling, Path.Combine(destinationFolder, newStem + name.Substring(oldStem.Length))));
            }

            foreach (var (_, target) in plan)
                if (File.Exists(target) || Directory.Exists(target))
                    throw new IOException(
                        $"Could not move the library: {target} already exists. Nothing was moved. Move or rename that file, or choose another folder.");

            Directory.CreateDirectory(destinationFolder);
            var done = new List<(string From, string To)>();
            try
            {
                foreach (var step in plan)
                {
                    File.Move(step.From, step.To, false);
                    done.Add(step);
                }
            }
            catch (Exception e)
            {
                foreach (var step in Enumerable.Reverse(done))
                {
                    try { File.Move(step.To, step.From, false); } catch { /* best effort: must not replace the real failure */ }
                }
                throw new IOException(
                    $"Could not move the library to {destination}: {e.Message} Nothing was moved, or what was moved has been put back.", e);
            }
            return true;
        }
    }
}
