// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.Icons;
using Rdmp.Core.Icons.IconProvision;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;

namespace Rdmp.Core.Tests.Icons;

internal class EmbeddedIconHelperTests
{
    private const string P = "Rdmp.Core.Icons.";

    private static string[] EmbeddedPngResourceNames() =>
        typeof(EmbeddedIconHelper).Assembly.GetManifestResourceNames()
            .Where(static n => n.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .ToArray();

    [Test]
    public void EveryEmbeddedIconResource_IsAValidPng()
    {
        var names = EmbeddedPngResourceNames();
        Assert.That(names, Has.Length.GreaterThan(1000), "Expected the embedded icons to be found");

        var notPng = names.Where(name =>
        {
            using var stream = typeof(EmbeddedIconHelper).Assembly.GetManifestResourceStream(name);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return !PngHeader.IsPng(ms.GetBuffer().AsSpan(0, (int)ms.Length));
        }).ToArray();

        Assert.That(notPng, Is.Empty,
            "These embedded .png resources are not actually PNG files (e.g. renamed BMPs), convert them to PNG");
    }

    [Test]
    public void EveryEmbeddedIconResource_IsReachable()
    {
        foreach (var name in EmbeddedPngResourceNames())
        {
            var icon = EmbeddedIconHelper.GetRequired(name[..^4]);
            Assert.That(icon.ResourceName, Is.EqualTo(name));
            Assert.That(EmbeddedIconHelper.GetPngBytes(icon), Is.Not.Empty);
        }
    }

    [Test]
    public void Get_IsCaseInsensitive()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EmbeddedIconHelper.Get($"{P}catalogue"), Is.EqualTo(CatalogueIcons.Catalogue));
            Assert.That(EmbeddedIconHelper.Get($"{P}CATALOGUE"), Is.SameAs(EmbeddedIconHelper.Get($"{P}Catalogue")));
        });
    }

    [Test]
    public void Get_Missing_ReturnsNull_GetRequired_Throws()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EmbeddedIconHelper.Get($"{P}NoSuchIconExists"), Is.Null);
            Assert.Throws<InvalidOperationException>(() => EmbeddedIconHelper.GetRequired($"{P}NoSuchIconExists"));
        });
    }

    [TestCase("Setting", "famfamfam.cog")]
    [TestCase("LoadMetadataVersionNode", "CatalogueFolder")]
    [TestCase("RegexRedaction", "StandardRegex3")]
    [TestCase("RegexRedactionConfiguration", "StandardRegex311")]
    [TestCase("RegexRedactionKey", "StandardRegex31")]
    public void Aliases_ResolveToTarget(string alias, string target)
    {
        var aliasIcon = EmbeddedIconHelper.GetRequired(P + alias);

        Assert.Multiple(() =>
        {
            Assert.That(aliasIcon, Is.EqualTo(EmbeddedIconHelper.GetRequired(P + target)));
            Assert.That(aliasIcon.ResourceName, Is.EqualTo($"{P}{target}.png"));
        });
    }

    [Test]
    public void GetPngBytes_ReturnsRawPng()
    {
        var bytes = EmbeddedIconHelper.GetPngBytes(CatalogueIcons.Catalogue);

        Assert.Multiple(() =>
        {
            Assert.That(PngHeader.IsPng(bytes));
            Assert.That(PngHeader.GetDimensions(bytes), Is.EqualTo((19, 19)));
        });
    }

    [Test]
    public void GetPngBytes_IgnoresOperations()
    {
        Assert.That(EmbeddedIconHelper.GetPngBytes(CatalogueIcons.Catalogue.WithOverlay(OverlayKind.Link).AsGreyscale()),
            Is.EqualTo(EmbeddedIconHelper.GetPngBytes(CatalogueIcons.Catalogue)));
    }

    [Test]
    public void OpenPngStream_NullIcon_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => EmbeddedIconHelper.OpenPngStream(null));
    }

    private enum IconsThatDoNotExist
    {
        Catalogue,
        NoSuchIconExistsForThisEnumValue
    }

    [Test]
    public void EnumImageCollection_MissingImages_Throws()
    {
        var ex = Assert.Throws<IconProvisionException>(() => _ = new EnumImageCollection<IconsThatDoNotExist>(P));
        Assert.That(ex.Message, Does.Contain(nameof(IconsThatDoNotExist.NoSuchIconExistsForThisEnumValue))
            .And.Not.Contain($"{Environment.NewLine}{nameof(IconsThatDoNotExist.Catalogue)}"));
    }

    [Test]
    public void EnumImageCollection_ReturnsIcons()
    {
        var collection = new EnumImageCollection<RDMPConcept>(P);

        Assert.Multiple(() =>
        {
            Assert.That(collection[RDMPConcept.Catalogue], Is.EqualTo(CatalogueIcons.Catalogue));
            Assert.That(collection.ToStringDictionary()[nameof(RDMPConcept.Catalogue)],
                Is.EqualTo(CatalogueIcons.Catalogue));
        });
    }
}
