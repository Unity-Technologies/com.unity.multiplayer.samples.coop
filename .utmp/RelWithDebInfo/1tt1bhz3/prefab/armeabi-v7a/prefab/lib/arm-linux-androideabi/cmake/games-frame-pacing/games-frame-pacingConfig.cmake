if(NOT TARGET games-frame-pacing::swappy)
add_library(games-frame-pacing::swappy SHARED IMPORTED)
set_target_properties(games-frame-pacing::swappy PROPERTIES
    IMPORTED_LOCATION "C:/Users/gregorykim/.gradle/caches/9.1.0/transforms/4ebbe5df63d1eb82789c97756ba50259/workspace/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy/libs/android.armeabi-v7a/libswappy.so"
    INTERFACE_INCLUDE_DIRECTORIES "C:/Users/gregorykim/.gradle/caches/9.1.0/transforms/4ebbe5df63d1eb82789c97756ba50259/workspace/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy/include"
    INTERFACE_LINK_LIBRARIES ""
)
endif()

if(NOT TARGET games-frame-pacing::swappy_static)
add_library(games-frame-pacing::swappy_static STATIC IMPORTED)
set_target_properties(games-frame-pacing::swappy_static PROPERTIES
    IMPORTED_LOCATION "C:/Users/gregorykim/.gradle/caches/9.1.0/transforms/4ebbe5df63d1eb82789c97756ba50259/workspace/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy_static/libs/android.armeabi-v7a/libswappy_static.a"
    INTERFACE_INCLUDE_DIRECTORIES "C:/Users/gregorykim/.gradle/caches/9.1.0/transforms/4ebbe5df63d1eb82789c97756ba50259/workspace/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy_static/include"
    INTERFACE_LINK_LIBRARIES ""
)
endif()

