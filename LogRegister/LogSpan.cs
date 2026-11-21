namespace LogRegister
{
    /// <summary>Identifies the semantic role of a range in rendered log text.</summary>
    public enum SpanType {         
        /// <summary>Timestamp of the event.</summary>
        Timestamp,
        /// <summary>Prefix identifying the event category.</summary>
        Prefix,
        /// <summary>Literal text from the template.</summary>
        NormalText,
        /// <summary>Value substituted for a template placeholder.</summary>
        Parameter
    }
    /// <summary>Describes one categorized range within a rendered log message.</summary>
    public struct LogSpan
    {
        /// <summary>Gets or sets the rendering category.</summary>
        public ILogCategory Category;
        /// <summary>Gets or sets the zero-based start index.</summary>
        public int Start;
        /// <summary>Gets or sets the span length.</summary>
        public int Length;
        /// <summary>Gets or sets the semantic span type.</summary>
        public SpanType Type;
    }
}
