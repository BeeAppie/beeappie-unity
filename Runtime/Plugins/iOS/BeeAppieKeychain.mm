// The Unity SDK's session in the iOS Keychain (SecretStore.cs), as the iOS SDK keeps it: this device only, readable after
// the first unlock (games refresh in the background), never in backups.
#import <Foundation/Foundation.h>
#import <Security/Security.h>

static NSMutableDictionary *BeeAppieKeychainQuery(const char *account)
{
    return [@{
        (__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
        (__bridge id)kSecAttrService: @"com.beeappie.unity",
        (__bridge id)kSecAttrAccount: [NSString stringWithUTF8String:account]
    } mutableCopy];
}

extern "C" {

int BeeAppieKeychainSet(const char *account, const char *value)
{
    if (account == NULL || value == NULL)
        return 0;
    NSMutableDictionary *query = BeeAppieKeychainQuery(account);
    NSDictionary *attributes = @{
        (__bridge id)kSecValueData: [[NSString stringWithUTF8String:value] dataUsingEncoding:NSUTF8StringEncoding],
        (__bridge id)kSecAttrAccessible: (__bridge id)kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
    };
    OSStatus status = SecItemUpdate((__bridge CFDictionaryRef)query, (__bridge CFDictionaryRef)attributes);
    if (status == errSecItemNotFound)
    {
        [query addEntriesFromDictionary:attributes];
        status = SecItemAdd((__bridge CFDictionaryRef)query, NULL);
    }
    return status == errSecSuccess ? 1 : 0;
}

// A copy the caller owns: Unity frees a returned string.
char *BeeAppieKeychainGet(const char *account)
{
    if (account == NULL)
        return NULL;
    NSMutableDictionary *query = BeeAppieKeychainQuery(account);
    query[(__bridge id)kSecReturnData] = @YES;
    query[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;
    CFTypeRef result = NULL;
    if (SecItemCopyMatching((__bridge CFDictionaryRef)query, &result) != errSecSuccess || result == NULL)
        return NULL;
    NSString *text = [[NSString alloc] initWithData:(__bridge_transfer NSData *)result encoding:NSUTF8StringEncoding];
    return text == nil ? NULL : strdup(text.UTF8String);
}

void BeeAppieKeychainDelete(const char *account)
{
    if (account != NULL)
        SecItemDelete((__bridge CFDictionaryRef)BeeAppieKeychainQuery(account));
}

}
