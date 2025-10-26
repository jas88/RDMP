// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStore.SciStoreServices81;
using SCIStorePlugin.Data;
using HICPluginTests;
using Rdmp.Core.Validation;
using Tests.Common;

namespace SCIStorePluginTests.Unit;

public class CodeValidationTests : DatabaseTests
{
    [OneTimeSetUp]
    public void BeforeAnyTests()
    {
        Validator.LocatorForXMLDeserialization = RepositoryLocator;
    }

    [Test]
    public void Test_TestResultWithOneUndefinedCodeBlockWhichIsALocalCode()
    {
        var testType = new TEST_TYPE
        {
            TestName = new[]
            {
                new CLINICAL_CIRCUMSTANCE_TYPE
                {
                    Item = new CLINICAL_INFORMATION_TYPE
                    {
                        ClinicalCode = new CLINICAL_CODE_TYPE
                        {
                            ClinicalCodeScheme = new CLINICAL_CODE_SCHEME_TYPE
                            {
                                ClinicalCodeSchemeId = "Undefined",
                                ClinicalCodeSchemeVersion = "Undefined"
                            },
                            ClinicalCodeValue = new [] {"NOT_A_READ_CODE"}
                        },
                        ClinicalCodeDescription = "This is a test"
                    }
                }
            }
        };


        var readCodeConstraint = new MockReferentialIntegrityConstraint(x=>x.Equals("NOT_A_READ_CODE",StringComparison.Ordinal)?"This is not a read code":null);
        var testSetFactory = new TestSetFactory(readCodeConstraint);
        var testDetails = testSetFactory.CreateFromTestType(testType, ThrowImmediatelyDataLoadEventListener.Quiet);

       Assert.That(testDetails.LocalCode.Value, Is.EqualTo("NOT_A_READ_CODE"));
        Assert.That(testDetails.ReadCode, Is.Null);
    }

    [Test]
    public void Test_TestResultWithOneUndefinedCodeBlockWhichIsAReadCode()
    {
        var testType = new TEST_TYPE
        {
            TestName = new[]
            {
                new CLINICAL_CIRCUMSTANCE_TYPE
                {
                    Item = new CLINICAL_INFORMATION_TYPE
                    {
                        ClinicalCode = new CLINICAL_CODE_TYPE
                        {
                            ClinicalCodeScheme = new CLINICAL_CODE_SCHEME_TYPE
                            {
                                ClinicalCodeSchemeId = "Undefined",
                                ClinicalCodeSchemeVersion = "Undefined"
                            },
                            ClinicalCodeValue = new [] {".0766"}
                        },
                        ClinicalCodeDescription = "This is a test"
                    }
                }
            }
        };


        var readCodeConstraint = new MockReferentialIntegrityConstraint();
        var testSetFactory = new TestSetFactory(readCodeConstraint);
        var testDetails = testSetFactory.CreateFromTestType(testType, ThrowImmediatelyDataLoadEventListener.Quiet);

        Assert.That(testDetails.ReadCode, Is.Not.Null);
       Assert.That(testDetails.ReadCode.Value, Is.EqualTo(".0766"));
        Assert.That(testDetails.LocalCode, Is.Null);
    }

    [Test]
    public void Test_TestResultWithTwoUndefinedCodeBlocksWhereOneIsReadAndTheOtherLocal()
    {
        var testType = new TEST_TYPE
        {
            TestName = new[]
            {
                new CLINICAL_CIRCUMSTANCE_TYPE
                {
                    Item = new CLINICAL_INFORMATION_TYPE
                    {
                        ClinicalCode = new CLINICAL_CODE_TYPE
                        {
                            ClinicalCodeScheme = new CLINICAL_CODE_SCHEME_TYPE
                            {
                                ClinicalCodeSchemeId = "Undefined",
                                ClinicalCodeSchemeVersion = "Undefined"
                            },
                            ClinicalCodeValue = new [] {"4Q24."}
                        },
                        ClinicalCodeDescription = "C-terminal glucagon level"
                    }
                },
                new CLINICAL_CIRCUMSTANCE_TYPE
                {
                    Item = new CLINICAL_INFORMATION_TYPE
                    {
                        ClinicalCode = new CLINICAL_CODE_TYPE
                        {
                            ClinicalCodeScheme = new CLINICAL_CODE_SCHEME_TYPE
                            {
                                ClinicalCodeSchemeId = "Undefined",
                                ClinicalCodeSchemeVersion = "Undefined"
                            },
                            ClinicalCodeValue = new [] {"GGOC"}
                        },
                        ClinicalCodeDescription = "C-terminal GLUCAGON"
                    }
                }
            }
        };

        var readCodeConstraint = new MockReferentialIntegrityConstraint(static x=>x.Equals("GGOC",StringComparison.Ordinal)?"Not a read code":null);
        var testSetFactory = new TestSetFactory(readCodeConstraint);
        var testDetails = testSetFactory.CreateFromTestType(testType, ThrowImmediatelyDataLoadEventListener.Quiet);

        Assert.That(testDetails.ReadCode,Is.Not.Null);
        Assert.That(testDetails.LocalCode,Is.Not.Null);

       Assert.That(testDetails.ReadCode.Value, Is.EqualTo("4Q24."));
       Assert.That(testDetails.LocalCode.Value, Is.EqualTo("GGOC"));
    }
}