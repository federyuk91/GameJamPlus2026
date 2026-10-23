using System;
using UnityEngine;

namespace GameJamPlus.Gameplay
{
    [Serializable]
    public sealed class UniverseDefinition
    {
        [SerializeField, Range(1, 10)] private int number;
        [SerializeField] private string sceneName;
        public int Number => number;
        public string SceneName => sceneName;

        public UniverseDefinition(int number, string sceneName)
        {
            this.number = number;
            this.sceneName = sceneName;
        }
    }

    [CreateAssetMenu(menuName = "Game/Universe Campaign")]
    public sealed class UniverseCampaign : ScriptableObject
    {
        [SerializeField, Min(1)] private int attemptsPerUniverse = 10;
        [SerializeField, Min(1)] private int maximumDeaths = 100;
        [SerializeField] private string endGameScene = "EndGame";
        [SerializeField] private UniverseDefinition[] universes =
        {
            new UniverseDefinition(10, "Universo_10"), new UniverseDefinition(9, "Universo_9"),
            new UniverseDefinition(8, "Universo_8"), new UniverseDefinition(7, "Universo_7"),
            new UniverseDefinition(6, "Universo_6"), new UniverseDefinition(5, "Universo_5"),
            new UniverseDefinition(4, "Universo_4"), new UniverseDefinition(3, "Universo_3"),
            new UniverseDefinition(2, "Universo_2"), new UniverseDefinition(1, "Universo_1")
        };

        public int AttemptsPerUniverse => Mathf.Max(1, attemptsPerUniverse);
        public int MaximumDeaths => Mathf.Max(1, maximumDeaths);
        public string EndGameScene => endGameScene;

        public string SceneForUniverse(int number)
        {
            foreach (UniverseDefinition universe in universes)
                if (universe != null && universe.Number == number) return universe.SceneName;
            return string.Empty;
        }
    }
}
