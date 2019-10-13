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

static uint32_t readInMainThread(char *output, int32_t output_len)
{
    memset(output, 0, output_len);
    __block int32_t output_offset = 0;
    __block BOOL overflowed  = NO;
    __block BOOL found = NO;
    [[[NSApplication sharedApplication] windows] enumerateObjectsUsingBlock:^(NSWindow *window, NSUInteger idx, BOOL *stop)
    {
        (void)idx;
        (void)stop;
        const char *title = [[window title] UTF8String];
        int32_t title_len = strlen(title);
        if (output_offset + title_len + 1 >= output_len)
        {
	        overflowed = YES;
        }
        else
        {
            memcpy(&output[output_offset], title, title_len);
            output[output_offset + title_len] = '\n';
            output_offset += title_len + 1;
        }
        
        found = YES;
    }];

    // Remove last newline char
    if (output_offset > 0 && output_offset <= output_len)
    {
        output[output_offset - 1] = 0;
    }

    if (!found)
    {
        return 1;
    }
    else if (overflowed)
    {
        return 2;
    }
    return 0;
}

uint32_t StandaloneWindowTitleChanger_Tests_ReadNative(char *output, int32_t output_len)
{
    if ([NSThread isMainThread])
    {
        return readInMainThread(output, output_len);
    }

    __block uint32_t read_result = 0;
    dispatch_sync(dispatch_get_main_queue(),
    ^{
        read_result = readInMainThread(output, output_len);
    });
    return read_result;
}
