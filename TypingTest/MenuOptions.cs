using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace TypingTest
{
    class MenuOption<T>
    {
        public T optionValue { get; set; }
        public String name { get; set; }
        public bool selected { get; set; }
        public MenuOption(T optionValue, String name, bool selected)
        {
            this.optionValue = optionValue;
            this.name = name;
            this.selected = selected;
        }
    }
    class MenuOptions<T> where T : struct, IConvertible
    {
        Action<T> selectedAction;
        String name;
        List<MenuOption<T>> options = new List<MenuOption<T>>();
        MenuFlyout menuFlyout;
        public MenuOptions(String name, MenuFlyout menuFlyout, Action<T> selectedAction)
        {
            this.name = name;
            this.menuFlyout = menuFlyout;
            this.selectedAction = selectedAction;
        }
        public void addOption(T optionValue, String name, bool selected)
        {
            options.Add(new MenuOption<T>(optionValue, name, selected));
        }
        public void updateMenu()
        {
            menuFlyout.Items.Clear();
            for(int i = 0; i < options.Count(); i++)
            {
                var item = new MenuFlyoutItem
                {
                    Name = name + "Item" + i,
                    Text = (options[i].selected ? "✓ " : "") + options[i].name
                };
                item.Click += (o, e) => {
                    select((o as MenuFlyoutItem).Name);
                    updateMenu();
                };
                menuFlyout.Items.Add(item);
            }
        }
        private void select(String key)
        {
            try
            {
                int index = int.Parse(key.Replace(name + "Item", ""));
                for (int i = 0; i < options.Count; i++)
                    options[i].selected = i == index;
                selectedAction(options[index].optionValue);
            }
            catch
            {
                Debug.WriteLine("Error parsing integer in MenuOptions");
            }
        }

    }
}
