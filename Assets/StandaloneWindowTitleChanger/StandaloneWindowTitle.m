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
#import <Cocoa/Cocoa.h>

static uint32_t changeInMainThread(const char *title)
{
    __block BOOL changed = NO;

    // We don't use [NSApplication mainWindow]. That is nil when the application is inactive or hidden.
    [[[NSApplication sharedApplication] windows] enumerateObjectsUsingBlock:^(NSWindow *window, NSUInteger idx, BOOL *stop)
    {
        (void)idx;
        (void)stop;

        if (![window isMainWindow])
        {
            return;
        }

        [window setTitle:[NSString stringWithUTF8String:title]];
        changed = YES;
    }];

    return changed ? 0 : 1;
}

uint32_t StandaloneWindowTitleChanger_StandaloneWindowTitle_ChangeNative(const char *title)
{
    if ([NSThread isMainThread])
    {
        return changeInMainThread(title);
    }

    __block uint32_t changeResult = 0;
    dispatch_sync(dispatch_get_main_queue(),
    ^{
        changeResult = changeInMainThread(title);
    });
    return changeResult;
}
