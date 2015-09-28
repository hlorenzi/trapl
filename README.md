# Trapl Programming Language
Trapl is intended as a "better C language". In C, you must deal with low-level runtime features, such as pointers and memory management, but you must also work with "low-level *compile-time* features" -- that is, making sure every file ```#include```s what is necessary (not to mention having to write separate header and implementation files!), and making sure declarations come before use. While that sort of structure makes parsing and compiling easier and more efficient (I think), it detracts from the actual logic of the program, and puts another burden on the programmer. The idea with Trapl is to offer low-level runtime features just like C, while improving compile-time convenience.

In Trapl, typical struct and function declarations look like:

```rust
Numbers
{
  x: Int,
  y: Float,
  z: Int64
}

add (x: Int, y: Int -> Int)
  { return x + y }
```

Please note that syntax is not final, and might change in the future.

Another example, using templates:

```rust
Container<gen T>
  { value: gen T }

put_in_container<gen T> (container: &Container<gen T>, value: gen T)
  { (@container).value = value }
```

Pointer-types are written as ```&Type``` to match the address-of operator ```&expr```. Also to note is that the dereference operator is written as ```@expr```.

Templates use pattern matching, so you can use arbitrarily complex expressions and extract inner types:

```rust
OnlyTakesPointers<&gen T>
  { pointer: &gen T }
  
OnlyTakesPointersAndUnwrapsThem<&gen T>
  { unwrapped: gen T }

OnlyTakesContainers<Container<gen T>>
  { container: Container<gen T> }

OnlyTakesContainersAndUnwrapsTheirValues<Container<gen T>>
  { value: gen T }
```

I think it is very nice that the name of your declarations is the first thing to appear in the line.
