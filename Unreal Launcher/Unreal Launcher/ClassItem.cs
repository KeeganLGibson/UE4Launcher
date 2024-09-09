// Copyright (c) Keegan L Gibson. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;

namespace Unreal_Launcher
{
	[Serializable]
	public class ClassItem
	{
		private ClassItem _parent;

		public ClassItem()
		{
			SubClasses = new List<ClassItem>();
		}

		public ClassItem(string className, string sourceFileLocation, bool isGameModule, ClassItem parent = null, string objectType = "class")
		{
			SubClasses = new List<ClassItem>();

			ObjectType = objectType;
			ClassName = className;
			SourceFileLocation = sourceFileLocation;

			IsGameModule = isGameModule;

			Parent = parent;
		}

		public ClassItem Parent
		{
			get => _parent;
			set
			{
				// Remove self from Old Parent
				if (_parent != null)
				{
					_parent.SubClasses.Remove(this);
				}

				_parent = value;

				// Add Self to new parent
				if (_parent != null)
				{
					_parent.SubClasses.Add(this);
				}
			}
		}

        public List<ClassItem> SubClasses { get; set; }
        public string ObjectType { get; set; }
		public string ClassName { get; set; }
		public string SourceFileLocation { get; set; }

		public bool IsGameModule { get; set; }

		public void PopulateItems(ItemCollection treeItemCollection)
		{
			foreach (ClassItem subClass in SubClasses)
			{
				TreeViewItem subClassItem = new TreeViewItem
				{
					Header = subClass.ClassName,
					Tag = subClass,
				};
				treeItemCollection.Add(subClassItem);

				subClass.PopulateItems(subClassItem.Items);
			}

			treeItemCollection.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));
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
			if (ClassName == className)
			{
				return this;
			}

			if (SubClasses != null)
			{
				foreach (ClassItem subClass in SubClasses)
				{
					ClassItem foundItem = subClass.FindClassByName(className);

					if (foundItem != null)
					{
						return foundItem;
					}
				}
			}

			return null;
		}
	}
}
