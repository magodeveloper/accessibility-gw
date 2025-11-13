using Xunit;
using FluentAssertions;
using Gateway.Models.Swagger.Intelligence;

namespace Gateway.UnitTests.Models
{
    /// <summary>
    /// Tests para modelos de Intelligence API
    /// Target: >80% coverage (validación de propiedades y comportamiento)
    /// </summary>
    public class IntelligenceModelsTests
    {
        #region AIRecommendationRequest Tests

        [Fact]
        public void AIRecommendationRequest_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var request = new AIRecommendationRequest
            {
                AnalysisData = new AnalysisDataDto
                {
                    AnalysisId = "analysis-123",
                    Url = "https://example.com",
                    AnalysisDate = DateTime.UtcNow,
                    Results = "{\"violations\":[]}"
                },
                UserLevel = "intermediate",
                UserLanguage = "es",
                MaxRecommendations = 10
            };

            // Assert
            request.AnalysisData.Should().NotBeNull();
            request.AnalysisData.AnalysisId.Should().Be("analysis-123");
            request.UserLevel.Should().Be("intermediate");
            request.UserLanguage.Should().Be("es");
            request.MaxRecommendations.Should().Be(10);
        }

        [Fact]
        public void AIRecommendationRequest_WithDefaultValues_ShouldHaveCorrectDefaults()
        {
            // Arrange & Act
            var request = new AIRecommendationRequest
            {
                AnalysisData = new AnalysisDataDto
                {
                    AnalysisId = "test",
                    Url = "https://test.com",
                    Results = "{}"
                }
            };

            // Assert
            request.UserLevel.Should().Be("intermediate", "nivel por defecto debe ser intermediate");
            request.UserLanguage.Should().Be("es", "idioma por defecto debe ser español");
            request.MaxRecommendations.Should().Be(10, "máximo por defecto debe ser 10");
        }

        [Theory]
        [InlineData("basic")]
        [InlineData("intermediate")]
        [InlineData("advanced")]
        public void AIRecommendationRequest_WithDifferentLevels_ShouldAcceptValidLevels(string level)
        {
            // Arrange & Act
            var request = new AIRecommendationRequest
            {
                AnalysisData = new AnalysisDataDto
                {
                    AnalysisId = "test",
                    Url = "https://test.com",
                    Results = "{}"
                },
                UserLevel = level
            };

            // Assert
            request.UserLevel.Should().Be(level);
        }

        #endregion

        #region AIRecommendationResponse Tests

        [Fact]
        public void AIRecommendationResponse_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var response = new AIRecommendationResponse
            {
                Recommendations = new List<AIRecommendation>
                {
                    new AIRecommendation
                    {
                        Id = "rec-1",
                        Title = "Mejorar contraste",
                        Description = "El texto debe tener mayor contraste",
                        Severity = "serious",
                        Category = "color-contrast",
                        Priority = 8
                    }
                },
                Summary = new AIRecommendationsSummary
                {
                    TotalIssues = 1,
                    CriticalIssues = 0,
                    SeriousIssues = 1
                },
                Statistics = new AIStatistics
                {
                    ComplianceScore = 85.5,
                    ComplianceLevel = "AA",
                    PassedCriteria = 45,
                    TotalCriteria = 50
                },
                CacheKey = "cache-123"
            };

            // Assert
            response.Recommendations.Should().HaveCount(1);
            response.Summary.TotalIssues.Should().Be(1);
            response.Statistics.ComplianceScore.Should().Be(85.5);
            response.CacheKey.Should().Be("cache-123");
        }

        [Fact]
        public void AIRecommendationResponse_WithEmptyRecommendations_ShouldAllowEmptyList()
        {
            // Arrange & Act
            var response = new AIRecommendationResponse
            {
                Recommendations = new List<AIRecommendation>(),
                Summary = new AIRecommendationsSummary(),
                Statistics = new AIStatistics()
            };

            // Assert
            response.Recommendations.Should().BeEmpty();
            response.CacheKey.Should().BeNull();
        }

        #endregion

        #region AIRecommendation Tests

        [Fact]
        public void AIRecommendation_WithCompleteData_ShouldInitializeAllProperties()
        {
            // Arrange & Act
            var recommendation = new AIRecommendation
            {
                Id = "rec-123",
                Title = "Agregar texto alternativo",
                Description = "Las imágenes deben tener atributo alt descriptivo",
                Severity = "critical",
                Category = "aria",
                Priority = 10,
                CodeExample = "<img src='logo.png' alt='Logo de la empresa'>",
                Resources = new List<AIResource>
                {
                    new AIResource
                    {
                        Title = "WCAG 1.1.1",
                        Url = "https://www.w3.org/WAI/WCAG21/Understanding/non-text-content.html",
                        Type = "documentation"
                    }
                },
                WcagCriterion = "1.1.1",
                WcagLevel = "A"
            };

            // Assert
            recommendation.Id.Should().Be("rec-123");
            recommendation.Title.Should().Contain("alternativo");
            recommendation.Severity.Should().Be("critical");
            recommendation.Category.Should().Be("aria");
            recommendation.Priority.Should().Be(10);
            recommendation.CodeExample.Should().Contain("alt");
            recommendation.Resources.Should().HaveCount(1);
            recommendation.WcagCriterion.Should().Be("1.1.1");
            recommendation.WcagLevel.Should().Be("A");
        }

        [Theory]
        [InlineData("critical", 10)]
        [InlineData("serious", 7)]
        [InlineData("moderate", 5)]
        [InlineData("minor", 3)]
        public void AIRecommendation_WithDifferentSeverities_ShouldMapToPriority(
            string severity, int expectedPriority)
        {
            // Arrange & Act
            var recommendation = new AIRecommendation
            {
                Id = "test",
                Title = "Test",
                Description = "Test description",
                Severity = severity,
                Category = "test",
                Priority = expectedPriority
            };

            // Assert
            recommendation.Severity.Should().Be(severity);
            recommendation.Priority.Should().Be(expectedPriority);
        }

        [Theory]
        [InlineData("color-contrast")]
        [InlineData("aria")]
        [InlineData("forms")]
        [InlineData("multimedia")]
        [InlineData("structure")]
        [InlineData("keyboard")]
        [InlineData("navigation")]
        public void AIRecommendation_WithDifferentCategories_ShouldAcceptValidCategories(string category)
        {
            // Arrange & Act
            var recommendation = new AIRecommendation
            {
                Id = "test",
                Title = "Test",
                Description = "Test",
                Severity = "moderate",
                Category = category
            };

            // Assert
            recommendation.Category.Should().Be(category);
        }

        #endregion

        #region AIResource Tests

        [Fact]
        public void AIResource_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var resource = new AIResource
            {
                Title = "WCAG Quick Reference",
                Url = "https://www.w3.org/WAI/WCAG21/quickref/",
                Type = "documentation"
            };

            // Assert
            resource.Title.Should().Be("WCAG Quick Reference");
            resource.Url.Should().Contain("w3.org");
            resource.Type.Should().Be("documentation");
        }

        [Theory]
        [InlineData("documentation")]
        [InlineData("tutorial")]
        [InlineData("article")]
        [InlineData("video")]
        public void AIResource_WithDifferentTypes_ShouldAcceptValidTypes(string type)
        {
            // Arrange & Act
            var resource = new AIResource
            {
                Title = "Resource",
                Url = "https://example.com",
                Type = type
            };

            // Assert
            resource.Type.Should().Be(type);
        }

        [Fact]
        public void AIResource_WithDefaultType_ShouldBeDocumentation()
        {
            // Arrange & Act
            var resource = new AIResource
            {
                Title = "Test",
                Url = "https://test.com"
            };

            // Assert
            resource.Type.Should().Be("documentation", "tipo por defecto debe ser documentation");
        }

        #endregion

        #region AIRecommendationsSummary Tests

        [Fact]
        public void AIRecommendationsSummary_WithValidCounts_ShouldCalculateCorrectly()
        {
            // Arrange & Act
            var summary = new AIRecommendationsSummary
            {
                TotalIssues = 25,
                CriticalIssues = 5,
                SeriousIssues = 10,
                ModerateIssues = 7,
                MinorIssues = 3,
                GeneralSummary = "Se encontraron 25 problemas de accesibilidad",
                MainRecommendation = "Priorizar corrección de problemas críticos"
            };

            // Assert
            summary.TotalIssues.Should().Be(25);
            summary.CriticalIssues.Should().Be(5);
            summary.SeriousIssues.Should().Be(10);
            summary.ModerateIssues.Should().Be(7);
            summary.MinorIssues.Should().Be(3);
            (summary.CriticalIssues + summary.SeriousIssues + summary.ModerateIssues + summary.MinorIssues)
                .Should().Be(summary.TotalIssues, "suma de severidades debe coincidir con total");
        }

        [Fact]
        public void AIRecommendationsSummary_WithZeroIssues_ShouldAllowZero()
        {
            // Arrange & Act
            var summary = new AIRecommendationsSummary
            {
                TotalIssues = 0,
                CriticalIssues = 0,
                SeriousIssues = 0,
                ModerateIssues = 0,
                MinorIssues = 0
            };

            // Assert
            summary.TotalIssues.Should().Be(0);
            summary.GeneralSummary.Should().BeNull();
            summary.MainRecommendation.Should().BeNull();
        }

        #endregion

        #region AIStatistics Tests

        [Fact]
        public void AIStatistics_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var statistics = new AIStatistics
            {
                ComplianceScore = 92.5,
                ComplianceLevel = "AA",
                PassedCriteria = 48,
                TotalCriteria = 52
            };

            // Assert
            statistics.ComplianceScore.Should().Be(92.5);
            statistics.ComplianceLevel.Should().Be("AA");
            statistics.PassedCriteria.Should().Be(48);
            statistics.TotalCriteria.Should().Be(52);
            statistics.PassedCriteria.Should().BeLessThanOrEqualTo(statistics.TotalCriteria);
        }

        [Theory]
        [InlineData(100.0, "AAA", 50, 50)]
        [InlineData(85.0, "AA", 42, 50)]
        [InlineData(60.0, "A", 30, 50)]
        [InlineData(0.0, null, 0, 50)]
        public void AIStatistics_WithDifferentScores_ShouldMapToComplianceLevels(
            double score, string? level, int passed, int total)
        {
            // Arrange & Act
            var statistics = new AIStatistics
            {
                ComplianceScore = score,
                ComplianceLevel = level,
                PassedCriteria = passed,
                TotalCriteria = total
            };

            // Assert
            statistics.ComplianceScore.Should().Be(score);
            statistics.ComplianceLevel.Should().Be(level);
            statistics.PassedCriteria.Should().Be(passed);
            statistics.TotalCriteria.Should().Be(total);
        }

        #endregion

        #region AnalysisDataDto Tests

        [Fact]
        public void AnalysisDataDto_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange
            var analysisDate = DateTime.UtcNow;
            var resultsJson = "{\"violations\":[],\"passes\":[],\"incomplete\":[]}";

            // Act
            var analysisData = new AnalysisDataDto
            {
                AnalysisId = "analysis-456",
                Url = "https://mi-sitio.com",
                AnalysisDate = analysisDate,
                Results = resultsJson
            };

            // Assert
            analysisData.AnalysisId.Should().Be("analysis-456");
            analysisData.Url.Should().Be("https://mi-sitio.com");
            analysisData.AnalysisDate.Should().Be(analysisDate);
            analysisData.Results.Should().Be(resultsJson);
        }

        [Theory]
        [InlineData("https://example.com")]
        [InlineData("https://www.mi-sitio.com/page")]
        [InlineData("http://localhost:3000")]
        public void AnalysisDataDto_WithDifferentUrls_ShouldAcceptValidUrls(string url)
        {
            // Arrange & Act
            var analysisData = new AnalysisDataDto
            {
                AnalysisId = "test",
                Url = url,
                Results = "{}"
            };

            // Assert
            analysisData.Url.Should().Be(url);
        }

        #endregion

        #region SingleRecommendationRequest Tests

        [Fact]
        public void SingleRecommendationRequest_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var request = new SingleRecommendationRequest
            {
                Error = new ErrorInfo
                {
                    Code = "color-contrast",
                    Message = "Contraste insuficiente entre texto y fondo",
                    Impact = "serious",
                    WcagCriterion = "1.4.3",
                    HtmlContext = "<div style='color:#999'>Texto</div>"
                },
                UserLevel = "intermediate",
                Language = "es"
            };

            // Assert
            request.Error.Should().NotBeNull();
            request.Error.Code.Should().Be("color-contrast");
            request.UserLevel.Should().Be("intermediate");
            request.Language.Should().Be("es");
        }

        [Fact]
        public void SingleRecommendationRequest_WithDefaultValues_ShouldHaveCorrectDefaults()
        {
            // Arrange & Act
            var request = new SingleRecommendationRequest
            {
                Error = new ErrorInfo
                {
                    Code = "test",
                    Message = "test",
                    Impact = "moderate"
                }
            };

            // Assert
            request.UserLevel.Should().Be("intermediate");
            request.Language.Should().Be("es");
        }

        #endregion

        #region ErrorInfo Tests

        [Fact]
        public void ErrorInfo_WithCompleteData_ShouldInitializeAllProperties()
        {
            // Arrange & Act
            var errorInfo = new ErrorInfo
            {
                Code = "aria-required-attr",
                Message = "Falta atributo aria-label requerido",
                Impact = "critical",
                WcagCriterion = "4.1.2",
                HtmlContext = "<button>Click me</button>"
            };

            // Assert
            errorInfo.Code.Should().Be("aria-required-attr");
            errorInfo.Message.Should().Contain("aria-label");
            errorInfo.Impact.Should().Be("critical");
            errorInfo.WcagCriterion.Should().Be("4.1.2");
            errorInfo.HtmlContext.Should().Contain("button");
        }

        [Fact]
        public void ErrorInfo_WithMinimalData_ShouldAllowNullOptionalFields()
        {
            // Arrange & Act
            var errorInfo = new ErrorInfo
            {
                Code = "test-error",
                Message = "Error de prueba",
                Impact = "minor"
            };

            // Assert
            errorInfo.Code.Should().Be("test-error");
            errorInfo.Message.Should().Be("Error de prueba");
            errorInfo.Impact.Should().Be("minor");
            errorInfo.WcagCriterion.Should().BeNull();
            errorInfo.HtmlContext.Should().BeNull();
        }

        [Theory]
        [InlineData("critical")]
        [InlineData("serious")]
        [InlineData("moderate")]
        [InlineData("minor")]
        public void ErrorInfo_WithDifferentImpacts_ShouldAcceptValidImpacts(string impact)
        {
            // Arrange & Act
            var errorInfo = new ErrorInfo
            {
                Code = "test",
                Message = "test",
                Impact = impact
            };

            // Assert
            errorInfo.Impact.Should().Be(impact);
        }

        #endregion

        #region Collection Tests

        [Fact]
        public void AIRecommendationResponse_WithMultipleRecommendations_ShouldHandleCollection()
        {
            // Arrange & Act
            var response = new AIRecommendationResponse
            {
                Recommendations = new List<AIRecommendation>
                {
                    new AIRecommendation
                    {
                        Id = "rec-1",
                        Title = "Title 1",
                        Description = "Desc 1",
                        Severity = "critical",
                        Category = "aria"
                    },
                    new AIRecommendation
                    {
                        Id = "rec-2",
                        Title = "Title 2",
                        Description = "Desc 2",
                        Severity = "serious",
                        Category = "color-contrast"
                    }
                },
                Summary = new AIRecommendationsSummary(),
                Statistics = new AIStatistics()
            };

            // Assert
            response.Recommendations.Should().HaveCount(2);
            response.Recommendations[0].Id.Should().Be("rec-1");
            response.Recommendations[1].Id.Should().Be("rec-2");
        }

        [Fact]
        public void AIRecommendation_WithMultipleResources_ShouldHandleCollection()
        {
            // Arrange & Act
            var recommendation = new AIRecommendation
            {
                Id = "test",
                Title = "Test",
                Description = "Test",
                Severity = "moderate",
                Category = "test",
                Resources = new List<AIResource>
                {
                    new AIResource { Title = "W3C", Url = "https://w3.org" },
                    new AIResource { Title = "MDN", Url = "https://mdn.org" },
                    new AIResource { Title = "WebAIM", Url = "https://webaim.org" }
                }
            };

            // Assert
            recommendation.Resources.Should().HaveCount(3);
            recommendation.Resources.Should().OnlyContain(r => !string.IsNullOrEmpty(r.Url));
        }

        #endregion
    }
}
