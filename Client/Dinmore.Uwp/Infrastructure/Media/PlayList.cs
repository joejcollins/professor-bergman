using System.Collections.Generic;

namespace Dinmore.Uwp.Infrastructure.Media
{
    public static class PlayList
    {
        public static List<PlayListItem> List
        {
            get
            {
                return new List<PlayListItem>
            {
                new PlayListItem( PlayListGroup.SingleFace, 1, "Assets/Voice/0-12-17-hiya.wav"),
                new PlayListItem( PlayListGroup.SingleFace, 2, "Assets/Voice/goat.wav"),
                new PlayListItem( PlayListGroup.SingleFace, 3, "Assets/Voice/sheep.wav"),
                new PlayListItem( PlayListGroup.MultiFace, 1, "Assets/Voice/0-12-17-helloeveryone.wav"),
            };
            }
        }
    }
}
