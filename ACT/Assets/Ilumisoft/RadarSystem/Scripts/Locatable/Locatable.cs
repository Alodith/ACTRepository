using Ilumisoft.RadarSystem.UI;
using UnityEngine;

namespace Ilumisoft.RadarSystem
{
    [AddComponentMenu("Radar System/Locatable")]
    public class Locatable : LocatableComponent
    {
        public bool isPinged = false;

        public override bool IsPinged { get => isPinged; set => isPinged = value; }

        [SerializeField]
        protected LocatableIconComponent iconPrefab;

        [SerializeField, Tooltip("Determines whether the locatable will be hidden or will stay visible when being out of the radar radius")]
        private bool clampOnRadar = false;

        public override bool ClampOnRadar { get => clampOnRadar; set => clampOnRadar = value; }

        public override LocatableIconComponent CreateIcon()
        {
            return Instantiate(iconPrefab);
        }

        
    }
}