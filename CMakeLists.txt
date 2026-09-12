cmake_minimum_required(VERSION 3.15)
project(PortForwarderKernels CXX)

# C++ port forwarding kernel. The output name "forward_kernel" matches the
# file name the GUI and kernel/middle.js expect (forward_kernel.exe).
add_executable(forward_kernel src/forward_kernel_win.cpp)

target_link_libraries(forward_kernel PRIVATE ws2_32)

set_target_properties(forward_kernel PROPERTIES
    CXX_STANDARD 11
    CXX_STANDARD_REQUIRED ON
    CXX_EXTENSIONS OFF)

if(MSVC)
    target_compile_options(forward_kernel PRIVATE /W4)
else()
    # -static so the exe can be copied into gui/Resources without MinGW runtime DLLs.
    target_compile_options(forward_kernel PRIVATE -static -Wall -Wextra)
    target_link_options(forward_kernel PRIVATE -static)
endif()
