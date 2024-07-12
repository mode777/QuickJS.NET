cmake_minimum_required(VERSION 3.5)
project(libquickjs)
set(CMAKE_C_STANDARD 11)

# Linux -pthread shenanigans
if (CMAKE_SYSTEM_NAME STREQUAL Linux)
    set(THREADS_PREFER_PTHREAD_FLAG ON)
    find_package(Threads REQUIRED)
endif()

message("=== Configure QuickJS ===")
# add quickjs
set(quickjs_version 2024-05-30)
set(quickjs_sources_root ${CMAKE_CURRENT_SOURCE_DIR}/quickjs)
set(quickjs_sources
	${quickjs_sources_root}/cutils.c
	${quickjs_sources_root}/cutils.h
	${quickjs_sources_root}/libbf.c
	${quickjs_sources_root}/libbf.h
	${quickjs_sources_root}/libregexp.c
	${quickjs_sources_root}/libregexp.h
	${quickjs_sources_root}/libunicode.h
	${quickjs_sources_root}/libunicode.c
	${quickjs_sources_root}/quickjs.h
	${quickjs_sources_root}/quickjs.c
	${quickjs_sources_root}/quickjs-libc.c
)
add_library(quickjs SHARED ${quickjs_sources})
target_compile_definitions(quickjs
	PRIVATE
		CONFIG_VERSION="${quickjs_version}"
        # See https://github.com/bellard/quickjs/issues/241
        CONFIG_BIGNUM=1
        DUMP_LEAKS=1
)
target_compile_options(quickjs PRIVATE -Wall -Wno-unused-parameter -Wno-unused-variable)
target_include_directories(quickjs
	PRIVATE
		${quickjs_sources_root}
)