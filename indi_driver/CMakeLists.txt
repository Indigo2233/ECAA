cmake_minimum_required(VERSION 3.5)
project(indi_caa_rotator CXX)

find_package(INDI REQUIRED)
include_directories(${INDI_INCLUDE_DIR})

add_executable(indi_caa_rotator indi_ecaa_rotator.cpp)

target_link_libraries(indi_caa_rotator
    ${INDI_LIBRARIES}
    ${CMAKE_THREAD_LIBS_INIT}
)

install(TARGETS indi_caa_rotator
    RUNTIME DESTINATION ${CMAKE_INSTALL_PREFIX}/bin
)

# INDI driver XML descriptor
set(DRIVER_XML ${CMAKE_CURRENT_BINARY_DIR}/drivers.xml)
file(WRITE ${DRIVER_XML}
"<?xml version=\"1.0\" encoding=\"UTF-8\"?>
<driversList>
  <devGroup group=\"Rotator\">
    <device label=\"CAA Rotator\" manufacturer=\"ECAA\">
      <driver name=\"CAA Rotator\">indi_caa_rotator</driver>
      <version>1.0.0</version>
    </device>
  </devGroup>
</driversList>
")

install(FILES ${DRIVER_XML}
    DESTINATION ${CMAKE_INSTALL_PREFIX}/share/indi
    RENAME drivers_caa.xml
)
