// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rdmp.Core.Icons;
using Rdmp.Core.Icons.IconOverlays;
using Rdmp.Core.Icons.IconProvision;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;

namespace Rdmp.Core.Tests.Icons;

internal class RdmpIconTests
{
    [Test]
    public void BaseIcon_HasNoOperations()
    {
        var icon = CatalogueIcons.Catalogue;

        Assert.Multiple(() =>
        {
            Assert.That(icon.ResourceName, Is.EqualTo("Rdmp.Core.Icons.Catalogue.png"));
            Assert.That(icon.Operations, Is.Empty);
        });
    }

    [Test]
    public void Equality_IsStructural()
    {
        var a = CatalogueIcons.Catalogue.WithOverlay(OverlayKind.Link).AsGreyscale();
        var b = CatalogueIcons.Catalogue.WithOverlay(OverlayKind.Link).AsGreyscale();

        Assert.Multiple(() =>
        {
            Assert.That(a, Is.Not.SameAs(b));
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b);
            Assert.That(a != b, Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.Equals((object)b));
        });
    }

    [Test]
    public void Equality_DiffersByBaseOrOperations()
    {
        var plain = CatalogueIcons.Catalogue;
        var linked = plain.WithOverlay(OverlayKind.Link);

        Assert.Multiple(() =>
        {
            Assert.That(plain, Is.Not.EqualTo(CatalogueIcons.Project));
            Assert.That(linked, Is.Not.EqualTo(plain));
            Assert.That(linked, Is.Not.EqualTo(plain.WithOverlay(OverlayKind.Add)));
            Assert.That(linked, Is.Not.EqualTo(CatalogueIcons.Project.WithOverlay(OverlayKind.Link)));
            Assert.That(linked.AsGreyscale(), Is.Not.EqualTo(linked));
            Assert.That(plain.Equals(null), Is.False);

            // not compared through 'plain': a null comparison would suggest that the variable used above can be null
            Assert.That(CatalogueIcons.Catalogue == null, Is.False);
            Assert.That((RdmpIcon)null == null);
        });
    }

    [Test]
    public void Operations_AreOrdered()
    {
        var overlayThenGrey = CatalogueIcons.Catalogue.WithOverlay(OverlayKind.Add).AsGreyscale();
        var greyThenOverlay = CatalogueIcons.Catalogue.AsGreyscale().WithOverlay(OverlayKind.Add);

        Assert.Multiple(() =>
        {
            Assert.That(overlayThenGrey, Is.Not.EqualTo(greyThenOverlay));
            Assert.That(overlayThenGrey.Operations, Is.EqualTo(new[]
            {
                RdmpIconOperation.Overlay(IconOverlayProvider.GetOverlayIcon(OverlayKind.Add)),
                RdmpIconOperation.Greyscale
            }));
            Assert.That(greyThenOverlay.Operations, Is.EqualTo(new[]
            {
                RdmpIconOperation.Greyscale,
                RdmpIconOperation.Overlay(IconOverlayProvider.GetOverlayIcon(OverlayKind.Add))
            }));
        });
    }

    [Test]
    public void Operations_DoNotMutateOriginal()
    {
        var plain = CatalogueIcons.Catalogue;
        var linked = plain.WithOverlay(OverlayKind.Link);
        _ = linked.WithOverlay(OverlayKind.Add);
        _ = linked.AsGreyscale();

        Assert.Multiple(() =>
        {
            Assert.That(plain.Operations, Is.Empty);
            Assert.That(linked.Operations, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void WithOverlay_None_ReturnsSameIcon()
    {
        var icon = CatalogueIcons.Catalogue;
        Assert.That(icon.WithOverlay(OverlayKind.None), Is.SameAs(icon));
    }

    [Test]
    public void AsGreyscale_IsIdempotent()
    {
        var grey = CatalogueIcons.Catalogue.AsGreyscale();
        Assert.That(grey.AsGreyscale(), Is.SameAs(grey));
    }

    [Test]
    public void WithOverlay_CustomIcon()
    {
        var custom = CatalogueIcons.Catalogue.WithOverlay(CatalogueIcons.Project.WithOverlay(OverlayKind.Link));

        Assert.Multiple(() =>
        {
            Assert.That(custom.Operations, Has.Count.EqualTo(1));
            Assert.That(custom.Operations[0].Kind, Is.EqualTo(RdmpIconOperationKind.Overlay));
            Assert.That(custom.Operations[0].OverlayIcon,
                Is.EqualTo(CatalogueIcons.Project.WithOverlay(OverlayKind.Link)));
            Assert.That(custom, Is.EqualTo(IconOverlayProvider.GetOverlay(CatalogueIcons.Catalogue,
                CatalogueIcons.Project.WithOverlay(OverlayKind.Link))));
            Assert.Throws<ArgumentNullException>(() => CatalogueIcons.Catalogue.WithOverlay((RdmpIcon)null));
        });
    }

    [Test]
    public void IconOverlayProvider_MatchesIconMethods()
    {
        var icon = CatalogueIcons.TableInfo;

        Assert.Multiple(() =>
        {
            Assert.That(IconOverlayProvider.GetOverlay(icon, OverlayKind.Problem),
                Is.EqualTo(icon.WithOverlay(OverlayKind.Problem)));
            Assert.That(IconOverlayProvider.GetGreyscale(icon), Is.EqualTo(icon.AsGreyscale()));
            Assert.That(IconOverlayProvider.GetOverlayIcon(OverlayKind.Problem).ResourceName,
                Is.EqualTo("Rdmp.Core.Icons.Overlays.Problem.png"));
        });
    }

    [Test]
    public void CanBeUsedAsDictionaryKey()
    {
        var dict = new Dictionary<RdmpIcon, int>
        {
            [CatalogueIcons.Catalogue.WithOverlay(OverlayKind.Link)] = 1,
            [CatalogueIcons.Catalogue.AsGreyscale()] = 2
        };

        Assert.Multiple(() =>
        {
            Assert.That(dict[CatalogueIcons.Catalogue.WithOverlay(OverlayKind.Link)], Is.EqualTo(1));
            Assert.That(dict[CatalogueIcons.Catalogue.AsGreyscale()], Is.EqualTo(2));
            Assert.That(dict.ContainsKey(CatalogueIcons.Catalogue), Is.False);
        });
    }

    [Test]
    public void ToString_DescribesOperations()
    {
        var s = CatalogueIcons.Catalogue.WithOverlay(OverlayKind.Link).AsGreyscale().ToString();

        Assert.That(s, Does.Contain("Rdmp.Core.Icons.Catalogue.png").And.Contain("Overlays.Link.png")
            .And.Contain("Greyscale"));
    }
}
