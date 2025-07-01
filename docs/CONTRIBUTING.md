# Contributing

## Development Setup

```bash
# Fork and clone
git clone https://github.com/yourusername/Summer-Chill.git
cd Summer-Chill

# Setup configuration
cp appsettings.template.json appsettings.Development.json
# Add your test credentials to appsettings.Development.json

# Build and test
dotnet restore
dotnet build
dotnet test
```

## Making Changes

1. Create a feature branch: `git checkout -b feature/your-feature`
2. Make your changes
3. Add tests for new functionality
4. Ensure all tests pass: `dotnet test`
5. Submit a pull request

## Code Standards

- Follow .NET coding conventions
- Add XML documentation for public APIs
- Include unit tests for new features
- Use structured logging with `ILogger`
- Handle exceptions appropriately

## Project Structure

```
├── Program.cs              # Entry point
├── Worker.cs               # Main background service
├── FGLairSettings.cs       # Configuration model
├── Services/               # Business logic
│   ├── IFGLairClient.cs    # API client interface
│   ├── FGLairClient.cs     # HTTP client implementation
│   └── DeviceInfo.cs       # Device management
└── Models/                 # Data models
```

## Testing

Run tests before submitting:
```bash
dotnet test --verbosity normal
```

## Pull Request Guidelines

- Provide clear description of changes
- Reference any related issues
- Ensure CI passes
- Update documentation if needed

### C# Guidelines
- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and single-purpose
- Use dependency injection for testability

### Example Code Style
```csharp
/// <summary>
/// Sets the louver position for the specified device.
/// </summary>
/// <param name="position">Target louver position (0-8)</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>True if successful, false otherwise</returns>
public async Task<bool> SetLouverPositionAsync(int position, CancellationToken cancellationToken = default)
{
    if (position < 0 || position > 8)
    {
        throw new ArgumentOutOfRangeException(nameof(position), "Position must be between 0 and 8");
    }

    _logger.LogInformation("Setting louver to position {Position}", position);
    
    // Implementation here
}
```

## Testing Requirements

### Unit Tests
- All public methods must have unit tests
- Aim for >80% code coverage
- Use meaningful test names: `Method_Scenario_ExpectedResult`
- Mock external dependencies

### Integration Tests
- Test actual API integration with test accounts
- Verify Docker container functionality
- Test configuration scenarios

### Test Example
```csharp
[Fact]
public async Task SetLouverPositionAsync_ValidPosition_ReturnsTrue()
{
    // Arrange
    var mockClient = new Mock<IFGLairClient>();
    mockClient.Setup(x => x.SetPropertyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
              .ReturnsAsync(true);
    
    var service = new LouverService(mockClient.Object, Mock.Of<ILogger<LouverService>>());
    
    // Act
    var result = await service.SetLouverPositionAsync(7);
    
    // Assert
    Assert.True(result);
}
```

## Pull Request Process

### Before Submitting
1. **Create Feature Branch**: `git checkout -b feature/your-feature-name`
2. **Write Tests**: Ensure new code is tested
3. **Update Documentation**: Update relevant docs
4. **Test Locally**: Run all tests and verify functionality
5. **Check Security**: No credentials in code/commits

### PR Requirements
- **Clear Title**: Descriptive title explaining the change
- **Detailed Description**: What, why, and how of your changes
- **Issue Reference**: Link to related issues
- **Screenshots**: For UI changes (if applicable)
- **Testing Notes**: How reviewers can test your changes

### PR Template
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature  
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] No credentials in code
```

## Development Workflows

### Adding New Features
1. Create feature branch from `main`
2. Implement feature with tests
3. Update documentation
4. Submit PR for review

### Bug Fixes
1. Create bug fix branch from `main`
2. Write test that reproduces the bug
3. Fix the bug
4. Verify test passes
5. Submit PR

### Documentation Updates
1. Update relevant `.md` files
2. Verify links work
3. Check formatting
4. Submit PR

## Local Development Tips

### Running in Debug Mode
```bash
# Interactive debugging
dotnet run --debug

# With specific log level
dotnet run --environment Development --verbosity detailed
```

### Docker Development
```bash
# Build and test container locally
docker build -t fglaircontrol-dev .
docker run -e FGLair__EnableDebugLogging=true fglaircontrol-dev

# Use development compose file
docker-compose -f docker-compose.dev.yml up
```

### API Testing
Use the `.http` files in the `tests/` directory with VS Code REST Client:
- `tests/auth.http` - Authentication testing
- `tests/devices.http` - Device API testing
- `tests/louver.http` - Louver control testing

## Code Review Guidelines

### For Reviewers
- Check for security issues (credentials, injection vulnerabilities)
- Verify tests cover new functionality
- Ensure documentation is updated
- Test the changes locally when possible
- Provide constructive feedback

### For Contributors
- Respond to feedback promptly
- Ask questions if feedback is unclear
- Make requested changes in additional commits
- Update PR description if scope changes

## Release Process

### Version Numbering
We use [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backwards compatible)
- **PATCH**: Bug fixes

### Release Checklist
- [ ] All tests pass
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
- [ ] Version bumped
- [ ] Release notes prepared

## Getting Help

### Communication Channels
- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: Questions and community support
- **Pull Request Comments**: Code-specific discussions

### Resources
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [FGLair API Documentation](./API.md)

## Recognition

Contributors will be acknowledged in:
- `CONTRIBUTORS.md` file
- Release notes
- Project README

Thank you for contributing to FGLair Control! 🎉
