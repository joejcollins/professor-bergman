namespace Dinmore.Uwp.Infrastructure.Media
{
    public class PlayListItem
    {
        public PlayListItem(PlayListGroup playListGroup, int sequenceId, string name)
        {
            PlayListGroup = playListGroup;
            SequenceId = sequenceId;
            Name = name;
        }

        public PlayListGroup PlayListGroup { get; private set; }
        public long SequenceId { get; private set; }
        public string Name { get; private set; }
    }
}
