using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.NewLogSourceDialog
{
    public class PagePresentersRegistry : IPagePresentersRegistry
    {
        readonly Dictionary<string, Func<ILogProviderFactory, IPagePresenter>> factories =
            new Dictionary<string, Func<ILogProviderFactory, IPagePresenter>>();

        IPagePresenter IPagePresentersRegistry.CreatePagePresenter(ILogProviderFactory factory)
        {
            Func<ILogProviderFactory, IPagePresenter> uiFactoryMethod;
            if (!factories.TryGetValue(factory.UITypeKey, out uiFactoryMethod))
                return null;
            return uiFactoryMethod(factory);
        }

        void IPagePresentersRegistry.RegisterPagePresenterFactory(string key, Func<ILogProviderFactory, IPagePresenter> createUi)
        {
            factories[key] = createUi;
        }
    }
}
