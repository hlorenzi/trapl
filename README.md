# Trapl Programming Language
Trapl is intended as a "better C/C++ language". In C/C++, you must deal with low-level runtime features, such as pointers and memory management, but you must also work with "low-level *compile-time* features" -- that is, making sure every file ```#include```s what is necessary (not to mention having to write separate header and implementation files!), and making sure declarations come before use. While that sort of structure makes parsing and compiling easier and more efficient (from 1970's perspective), it detracts from the actual logic of the program, and puts another burden on the programmer. The idea with Trapl is to offer low-level runtime features just like C/C++, while improving compile-time convenience. I'd actually want Trapl to transpile into C/C++, to use it with today's toolchains.

I've been inspired by Rust's recursive-descent-parseable syntax, ownership/borrowing semantics, and type inference.

I'm still experimenting with how to write a full-fledged compiler, so there's been a lot of iterating, and it's far from finished.

In Trapl, typical struct and function declarations should look like:

```rust
Numbers: struct
{
  x: Int,
  y: Float32,
  z: Int64
}

add: fn(x: Int, y: Int) -> Int
  { x + y }
```