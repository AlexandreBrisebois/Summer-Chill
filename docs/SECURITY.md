# Security

## ⚠️ Critical Rules

1. **Never commit credentials to version control**
2. **Use environment variables in production**
3. **Monitor for credential exposure**

## Secure Configuration

### Environment Variables (Recommended)
```bash
# Production
export FGLair__Username="user@example.com"
export FGLair__Password="secure-password"
export FGLair__DeviceDsn="DSXXXXXXXXXXXXXXXX"
```

### Docker Secrets (Advanced)
```yaml
# docker-compose.yml
services:
  fglaircontrol:
    secrets:
      - fglair_username
      - fglair_password
    environment:
      - FGLair__Username_FILE=/run/secrets/fglair_username
      - FGLair__Password_FILE=/run/secrets/fglair_password

secrets:
  fglair_username:
    file: ./secrets/username.txt
  fglair_password:
    file: ./secrets/password.txt
```

## What NOT to Do

❌ **Don't do this:**
```json
{
  "FGLair": {
    "Username": "myemail@gmail.com",
    "Password": "mypassword123"
  }
}
```

✅ **Do this instead:**
```bash
export FGLair__Username="myemail@gmail.com"
export FGLair__Password="mypassword123"
```

## Best Practices

- **Rotate credentials** regularly
- **Use strong passwords** with 2FA when available
- **Monitor logs** for authentication failures
- **Separate** dev/test/prod credentials
- **Review** container logs for credential leaks

## Incident Response

If credentials are exposed:
1. **Immediately** change FGLair password
2. **Rotate** all related credentials
3. **Review** commit history and logs
4. **Update** documentation if needed
    file: ./secrets/username.txt
  fglair_password:
    file: ./secrets/password.txt
  fglair_dsn:
    file: ./secrets/dsn.txt
```

#### ❌ Avoid: Hardcoded Credentials
```json
// DON'T DO THIS
{
  "FGLair": {
    "Username": "myemail@gmail.com",    // ❌ Exposed in code
    "Password": "mypassword123",        // ❌ Security risk
    "DeviceDsn": "DS1234567890"         // ❌ Device identifier exposed
  }
}
```

## Application Security

### Authentication Security

#### Token Management
- **Automatic Refresh**: Tokens refreshed automatically before expiration (5-minute buffer)
- **Refresh Token Support**: Uses refresh tokens when available to avoid re-authentication
- **Secure Storage**: Tokens stored in memory only, never persisted to disk
- **Session Timeout**: Handles session expiration gracefully with automatic retry
- **401 Error Recovery**: Automatically detects and recovers from authentication failures
- **Token Cleanup**: Clears stale tokens on authentication errors
- **No Token Logging**: Authentication tokens never logged for security

#### API Security Headers
```csharp
// Automatically applied by the application
headers.Add("User-Agent", "FGLairControl/1.0");
headers.Add("X-API-Version", "1.0");
headers.Authorization = new AuthenticationHeaderValue("auth_token", token);
```

### Network Security

#### HTTPS Enforcement
- All API communication uses HTTPS
- Certificate validation enabled
- TLS 1.2+ required
- No HTTP fallback

#### Request Security
```csharp
// Security measures implemented:
// 1. Request timeout prevention
// 2. Request size limits
// 3. Rate limiting respect
// 4. Connection pooling security
```

#### Authentication Flow
The application implements a robust authentication flow that handles token expiration automatically:

```csharp
// Authentication flow implementation:
// 1. Check if current token is valid (5-minute expiration buffer)
// 2. If expired, attempt refresh using refresh token
// 3. If refresh fails or no refresh token, perform fresh login
// 4. All API calls automatically retry on 401 Unauthorized errors
// 5. Clear stale tokens and retry with fresh authentication on failure

public async Task LoginAsync(CancellationToken cancellationToken = default)
{
    if (!IsTokenExpiredOrExpiring())
        return; // Use existing valid token
    
    if (!string.IsNullOrEmpty(_refreshToken))
    {
        if (await TryRefreshTokenAsync(cancellationToken))
            return; // Successfully refreshed
    }
    
    await PerformFreshLoginAsync(cancellationToken); // Fresh login
}
```

### Input Validation

#### Configuration Validation
```csharp
// Automatic validation applied to:
// - Email format validation
// - DSN format validation  
// - Position range validation (0-8)
// - Interval range validation
```

#### API Response Validation
- JSON schema validation
- Response size limits
- Content type verification
- Malicious content filtering

## Container Security

### Image Security

#### Base Image Selection
```dockerfile
# Use official Microsoft images
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

# Security benefits:
# - Regular security updates
# - Minimal attack surface
# - Verified provenance
# - No unnecessary packages
```

#### Container Hardening
```dockerfile
# Non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup
USER appuser

# Read-only root filesystem
VOLUME ["/tmp", "/var/tmp"]

# Remove package managers
RUN rm -rf /var/cache/apk/*
```

### Runtime Security

#### Security Context
```yaml
# docker-compose.yml
services:
  fglaircontrol:
    security_opt:
      - no-new-privileges:true
    read_only: true
    tmpfs:
      - /tmp
      - /var/tmp
    cap_drop:
      - ALL
    cap_add:
      - NET_BIND_SERVICE
```

#### Resource Limits
```yaml
# Prevent resource exhaustion attacks
deploy:
  resources:
    limits:
      memory: 512M
      cpus: '0.50'
    reservations:
      memory: 128M
      cpus: '0.25'
```

## Logging Security

### Sensitive Data Protection

#### Automatic Scrubbing
```csharp
// The application automatically scrubs:
// - Passwords from all log output
// - Authentication tokens
// - Personal identifiable information
// - API keys and secrets
```

#### Safe Logging Practices
```csharp
// ✅ Safe logging
_logger.LogInformation("Authentication successful for user {Username}", 
    ScrubEmail(username));

// ❌ Unsafe logging
_logger.LogInformation("Login with {Username}:{Password}", username, password);
```

### Log Security

#### Log Access Control
- Restrict log file permissions
- Use centralized logging systems
- Implement log retention policies
- Monitor log access

#### Audit Trail
```csharp
// Security events logged:
// - Authentication attempts (success/failure)
// - Configuration changes
// - API access patterns
// - Error conditions
```

## Deployment Security

### Production Deployment Checklist

#### Pre-Deployment
- [ ] Credentials configured via environment variables
- [ ] Container security context configured
- [ ] Network security policies applied
- [ ] Logging configuration reviewed
- [ ] Resource limits set
- [ ] Health checks configured

#### Post-Deployment
- [ ] Verify no credentials in logs
- [ ] Test authentication flow
- [ ] Verify HTTPS communication
- [ ] Monitor resource usage
- [ ] Check error handling
- [ ] Validate log output

### Network Security

#### Container Networking
```yaml
# Isolate container network
networks:
  fglair-network:
    driver: bridge
    internal: false  # Allow external API access
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

#### Firewall Configuration
```bash
# Only allow necessary outbound connections
# HTTPS to FGLair API (443)
# DNS resolution (53)
# Block all other outbound traffic
```

## Monitoring and Alerting

### Security Monitoring

#### Key Security Metrics
- Authentication failure rates
- API error responses
- Network connection failures
- Resource usage anomalies
- Configuration changes

#### Alert Conditions
```yaml
# Critical Alerts
- Authentication failures > 3 in 5 minutes
- Service crash or restart
- Unexpected network connections
- Memory/CPU usage > 90%

# Warning Alerts  
- API rate limiting detected
- Configuration reload
- Network timeouts
- Disk usage > 80%
```

### Incident Response

#### Security Incident Checklist
1. **Immediate Actions**
   - Stop the service if compromised
   - Rotate credentials immediately
   - Check logs for unauthorized access
   - Isolate affected systems

2. **Investigation**
   - Analyze logs for attack patterns
   - Check for data exfiltration
   - Verify system integrity
   - Document findings

3. **Recovery**
   - Apply security patches
   - Update credentials
   - Restart services
   - Monitor for continued issues

4. **Post-Incident**
   - Update security procedures
   - Improve monitoring
   - Conduct security review
   - Update documentation

## Security Updates

### Regular Security Tasks

#### Weekly
- [ ] Review application logs for anomalies
- [ ] Check for security updates
- [ ] Verify backup integrity
- [ ] Monitor resource usage

#### Monthly
- [ ] Rotate credentials
- [ ] Update container base images
- [ ] Review security configurations
- [ ] Test incident response procedures

#### Quarterly
- [ ] Comprehensive security audit
- [ ] Penetration testing
- [ ] Update security documentation
- [ ] Train team on security practices

## Compliance Considerations

### Data Protection
- **Minimal Data Collection**: Only necessary data stored
- **Data Retention**: Logs rotated and deleted
- **Data Transmission**: Encrypted in transit
- **Access Control**: Principle of least privilege

### Privacy
- **User Consent**: Appropriate for device control
- **Data Anonymization**: Personal data scrubbed from logs
- **Third-party Sharing**: None beyond necessary API calls
- **User Rights**: Support for data deletion requests

## Security Contacts

### Reporting Security Issues
- **Email**: [security@yourproject.com](mailto:security@yourproject.com)
- **PGP Key**: Available at [security page URL]
- **Response Time**: 24-48 hours for initial response
- **Disclosure**: Coordinated disclosure preferred

### Security Resources
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [Docker Security Best Practices](https://docs.docker.com/engine/security/)
- [Container Security Guide](https://www.nist.gov/publications/application-container-security-guide)

Remember: Security is everyone's responsibility! 🔒
