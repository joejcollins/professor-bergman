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
                new PlayListItem( PlayListGroup.SingleFace, 2, "Assets/Voice/1-55-64.wav"),
                new PlayListItem( PlayListGroup.SingleFace, 3,  "Assets/Voice/2-55-64.wav"),
                new PlayListItem( PlayListGroup.SingleFace, 3,  "Assets/Voice/3-55-64.wav"),
                new PlayListItem( PlayListGroup.SingleFace, 3,  "Assets/Voice/4-55-64.wav"),
                new PlayListItem( PlayListGroup.MultiFace, 1, "Assets/Voice/0-12-17-helloeveryone.wav"),
            };
            }
        }
    }
}
