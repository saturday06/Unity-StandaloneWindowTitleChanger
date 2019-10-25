// This is free and unencumbered software released into the public domain.
//
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
//
// In jurisdictions that recognize copyright laws, the author or authors
// of this software dedicate any and all copyright interest in the
// software to the public domain. We make this dedication for the benefit
// of the public at large and to the detriment of our heirs and
// successors. We intend this dedication to be an overt act of
// relinquishment in perpetuity of all present and future rights to this
// software under copyright law.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
// OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// For more information, please refer to <http://unlicense.org>

#import <stdint.h>
#import <string.h>
#import <Cocoa/Cocoa.h>

static uint32_t readInMainThread(char *output, int32_t signedOutputLen)
{
    if (output == NULL)
    {
        return 3;
    }
    if (signedOutputLen < 0)
    {
        return 4;
    }
    size_t outputLen = (size_t)signedOutputLen;
    memset(output, 0, outputLen);
    __block BOOL found = NO;
    __block NSMutableString *outputString = [NSMutableString string];

    // We don't use [NSApplication mainWindow]. That is nil when the application is inactive or hidden.
    [[[NSApplication sharedApplication] windows] enumerateObjectsUsingBlock:^(NSWindow *window, NSUInteger idx, BOOL *stop)
    {
        (void)idx;
        (void)stop;

        if (![window isMainWindow])
        {
            return;
        }

        if ([outputString length] > 0)
        {
            [outputString appendString:@"\n"];
        }
        [outputString appendString:[window title]];

        found = YES;
    }];

    if (!found)
    {
        return 1;
    }

    const char *result = [outputString UTF8String];
    if (outputLen < strlen(result) + 1)
    {
        return 2;
    }
    strcpy(output, result);

    return 0;
}

uint32_t StandaloneWindowTitleChangerTests_ReadNative(char *output, int32_t outputLen)
{
    if ([NSThread isMainThread])
    {
        return readInMainThread(output, outputLen);
    }

    __block uint32_t readResult = 0;
    dispatch_sync(dispatch_get_main_queue(),
    ^{
        readResult = readInMainThread(output, outputLen);
    });
    return readResult;
}
