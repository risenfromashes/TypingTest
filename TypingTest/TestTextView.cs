using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Windows.Data.Text;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace TypingTest
{
    enum TextHighlightStyle
    {
        Default, Red, Blue, Green
    }
    struct TextArea : IEquatable<TextArea>
    {
        public int start, end;
        public TextHighlightStyle style;
        public TextArea(TextHighlightStyle style, int start, int end)
        {
            this.start = start; this.end = end; this.style = style;
        }
        public bool Equals(TextArea other)
        {
            return start == other.start && end == other.end && style == other.style;
        }
        public override bool Equals(object other)
        {
            if (other is TextArea)
                return Equals((TextArea)other);
            return false;
        }
        public static bool operator ==(TextArea one, TextArea two)
        {
            return one.Equals(two);
        }
        public static bool operator !=(TextArea one, TextArea two)
        {
            return !one.Equals(two);
        }
        public override int GetHashCode()
        {
            return start ^ end;
        }

    }

    class TextSpan
    {
        public List<TextArea> textAreas = new List<TextArea>();
        private Span span = new Span();
        public String text;
        public TextSpan(String text) { this.text = text; }
        public Span update()
        {
            span.Inlines.Clear();
            int at = 0;
            foreach(var area in textAreas)
            {
                if (area.start > area.end || area.start >= text.Length || area.end >= text.Length)
                    continue;
                at = area.end;
                var run = new Run();
                run.Text = text.Substring(area.start, area.end - area.start + 1);
                switch (area.style)
                {
                    case TextHighlightStyle.Red:
                        run.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                        break;
                    case TextHighlightStyle.Green:
                        run.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                        break;
                    case TextHighlightStyle.Blue:
                        run.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 255));
                        run.TextDecorations = TextDecorations.Underline;
                        break;
                    case TextHighlightStyle.Default:
                    default:
                        break;
                }
                span.Inlines.Add(run);
            }
            return span;
        }
        public static TextSpan Default(String text)
        {
            var textSpan = new TextSpan(text);
            textSpan.textAreas.Add(new TextArea(TextHighlightStyle.Default, 0, text.Length - 1));
            return textSpan;
        }
    }

    class TestTextView
    {
        private TextSpan[] textSpans = null;
        private RichTextBlock textBlock = null;
        public TestTextView(RichTextBlock textBlock)
        {
            this.textBlock = textBlock;
        }
        public void updateWords(String[] words)
        {
            Run run = new Run();
            textBlock.Blocks.Clear();
            var para = new Paragraph();
            textSpans = new TextSpan[words.Length];
            for(int i = 0; i < words.Length; i++)
            {
                var span = textSpans[i] = TextSpan.Default(words[i]);
                para.Inlines.Add(span.update());
                para.Inlines.Add(new Run { Text = " " });
            }
            textBlock.Blocks.Add(para);
        }
        public void update(int index, Action<TextSpan> action)
        {
            if (textSpans == null || index >= textSpans.Length) return;
            else
            {
                var span = textSpans[index];
                span.textAreas.Clear();
                action(span);
                span.update();
            }

        }
    }
}
