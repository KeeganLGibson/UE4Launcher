using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Unreal_Launcher
{
    [Serializable]
    class ClassItem
    {
        public string ClassName;
        public string SourceFileLocation;

        public ClassItem Parent;
        public List<ClassItem> SubClasses;

        public bool IsGameModule;

        public ClassItem(string className, string sourceFileLocation, bool bIsGameModule, ClassItem parent = null)
        {
            ClassName = className;
            SourceFileLocation = sourceFileLocation;

            SubClasses = new List<ClassItem>();

            IsGameModule = bIsGameModule;

            SetParent(parent);
        }

        public void PopulateItems(ItemCollection TreeItemCollection)
        {
            foreach (ClassItem SubClass in SubClasses)
            {
                TreeViewItem SubClassItem = new TreeViewItem();
                SubClassItem.Header = SubClass.ClassName;
                SubClassItem.Tag = SubClass;
                TreeItemCollection.Add(SubClassItem);

                SubClass.PopulateItems(SubClassItem.Items);
            }

            TreeItemCollection.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));
        }

        public void SetParent(ClassItem parent)
        {
            if (parent != null)
            {
                // Remove self from Old Parent
                if (Parent != null)
                {
                    Parent.SubClasses.Remove(this);
                }

                Parent = parent;

                // Add Self to new parent
                Parent.SubClasses.Add(this);
            }
        }

        public ClassItem FindClassByName(string className)
        {
            if(ClassName == className)
            {
                return this;
            }

            foreach (ClassItem SubClass in SubClasses)
            {
                ClassItem FoundItem = SubClass.FindClassByName(className);

                if(FoundItem != null)
                {
                    return FoundItem;
                }
            }

            return null;
        }

    }
}
