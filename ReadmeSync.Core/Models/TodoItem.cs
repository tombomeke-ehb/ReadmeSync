namespace ReadmeSync.Models
{
    /// <summary>
    /// A TODO, FIXME or HACK note found in a comment.
    /// </summary>
    public class TodoItem
    {
        /// <summary>TODO, FIXME or HACK.</summary>
        public string Tag { get; set; } = "TODO";

        public string Text { get; set; } = "";

        /// <summary>1-based line of the comment.</summary>
        public int Line { get; set; }
    }
}
