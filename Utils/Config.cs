using System.IO;
using UnityEngine;

namespace ExtendedExile.Utils
{
    public class Config(string path)
    {
        public int MaxPlayers { get; private set; } = 8;

        public void Load()
        {
            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    if (line.StartsWith("MaxPlayers=") &&
                        int.TryParse(line.Split('=')[1], out var value))
                        MaxPlayers = Mathf.Clamp(value, 1, 20);
                }
            }
            else
            {
                File.WriteAllText(
                    path,
                    "# Max allowed players (1–20)\nMaxPlayers=" + MaxPlayers
                );
            }
        }
    }
}